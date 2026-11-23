using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using InkCanvasPlus.Helpers;

namespace InkCanvasPlus
{
    // 1.0.1.50：10MB+ 大笔记的渐进加载。
    // 真机基线（34,000 笔 / 10.7MB）：解析 1.5s、Strokes.Clear 约 15s、Strokes.Add 约 6s，
    // 原流程一次性做完会让界面冻结约 20s。这里改为分块清屏 + 分块添加，每块之间泵一次消息，
    // 让窗口保持重绘、能响应输入，同时用遮罩防止用户在加载中点按钮造成状态错乱。
    public partial class MainWindow
    {
        private const int StbPerfClearChunk = 300;
        private const int StbPerfAddChunk = 300;

        private bool _stbPerfLoadingInk;
        private Border _stbPerfBusyLayer;
        private TextBlock _stbPerfBusyText;

        private void StbPerfOnLoaded(object sender, RoutedEventArgs e)
        {
            string path = StbPerfGetStartupInkPath();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                // 等窗口完成首帧，再开始读文件，避免启动时一片白。
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
                {
                    StbLoadInkFile(path, true);
                }));
            }
        }

        private static string StbPerfGetStartupInkPath()
        {
            string[] args = App.StartArgs;
            if (args == null)
            {
                return null;
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-icstk", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
                if (args[i] != null && args[i].StartsWith("-icstk=", StringComparison.OrdinalIgnoreCase))
                {
                    return args[i].Substring("-icstk=".Length);
                }
            }
            return null;
        }

        /// <summary>让 Dispatcher 处理一轮输入/渲染，再继续下一块，避免整段冻结。</summary>
        private void StbPerfPump()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
            {
                frame.Continue = false;
            }));
            Dispatcher.PushFrame(frame);
        }

        private void StbPerfShowBusy(string text)
        {
            if (_stbPerfBusyLayer == null)
            {
                _stbPerfBusyLayer = new Border();
                _stbPerfBusyLayer.Background = new SolidColorBrush(Color.FromArgb(0x30, 0, 0, 0));
                _stbPerfBusyLayer.IsHitTestVisible = true;
                StackPanel panel = new StackPanel();
                panel.HorizontalAlignment = HorizontalAlignment.Center;
                panel.VerticalAlignment = VerticalAlignment.Center;
                _stbPerfBusyText = new TextBlock();
                _stbPerfBusyText.Foreground = Brushes.White;
                _stbPerfBusyText.FontSize = 22;
                _stbPerfBusyText.TextAlignment = TextAlignment.Center;
                _stbPerfBusyText.FontWeight = FontWeights.SemiBold;
                panel.Children.Add(_stbPerfBusyText);
                _stbPerfBusyLayer.Child = panel;
                Main_Grid.Children.Add(_stbPerfBusyLayer);
            }
            _stbPerfBusyText.Text = text;
            _stbPerfBusyLayer.Visibility = Visibility.Visible;
        }

        private void StbPerfHideBusy()
        {
            if (_stbPerfBusyLayer != null)
            {
                _stbPerfBusyLayer.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>读取 .icstk；兼容极少数被上游写成“空集合”的文件。</summary>
        private StrokeCollection StbPerfReadInk(string path, out long bytes)
        {
            byte[] data = File.ReadAllBytes(path);
            bytes = data.Length;
            using (MemoryStream ms = new MemoryStream(data))
            {
                StrokeCollection strokes = new StrokeCollection(ms);
                if (strokes.Count > 0)
                {
                    return strokes;
                }
            }
            using (MemoryStream ms = new MemoryStream(data))
            {
                return new StrokeCollection(ms);
            }
        }

        private static void StbPerfHookStroke(Stroke stroke, MainWindow owner)
        {
            // 只补事件订阅与初始点集的“引用”，不 Clone：34k 笔的 Clone 会白占几十 MB。
            // Stroke_StylusPointsChanged 会读 StrokeInitialHistory[sender]，这里必须建键，否则大笔记里操作笔迹会 KeyNotFound。
            owner.StrokeInitialHistory[stroke] = stroke.StylusPoints;
            stroke.StylusPointsChanged += owner.Stroke_StylusPointsChanged;
            stroke.StylusPointsReplaced += owner.Stroke_StylusPointsReplaced;
            stroke.DrawingAttributesChanged += owner.Stroke_DrawingAttributesChanged;
        }

        private void StbLoadInkFile(string path, bool isStartup)
        {
            if (_stbPerfLoadingInk)
            {
                return;
            }
            _stbPerfLoadingInk = true;
            Stopwatch totalWatch = Stopwatch.StartNew();
            long totalMs = 0;
            long parseMs = 0;
            long clearMs = 0;
            long addMs = 0;
            long bytes = 0;
            int total = 0;

            Cursor previousCursor = Mouse.OverrideCursor;
            bool previousHitTest = inkCanvas.IsHitTestVisible;
            bool handlerDetached = false;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                inkCanvas.IsHitTestVisible = false;
                StbPerfShowBusy("正在读取笔记文件…");
                StbPerfPump();

                Stopwatch phase = Stopwatch.StartNew();
                StrokeCollection loaded = StbPerfReadInk(path, out bytes);
                parseMs = phase.ElapsedMilliseconds;
                total = loaded == null ? 0 : loaded.Count;

                // 清空旧画布：分块移除，块间泵消息。先摘掉集合处理器，避免 34k 次历史提交。
                StbPerfShowBusy(string.Format("正在整理现有墨迹…（0 / {0}）", total));
                StbPerfPump();
                inkCanvas.Strokes.StrokesChanged -= StrokesOnStrokesChanged;
                handlerDetached = true;
                phase.Restart();
                while (inkCanvas.Strokes.Count > 0)
                {
                    int count = Math.Min(StbPerfClearChunk, inkCanvas.Strokes.Count);
                    StrokeCollection part = new StrokeCollection();
                    for (int i = 0; i < count; i++)
                    {
                        part.Add(inkCanvas.Strokes[i]);
                    }
                    inkCanvas.Strokes.Remove(part);
                    StbPerfPump();
                }
                clearMs = phase.ElapsedMilliseconds;

                // 旧笔迹的初始点集引用在这里一次性释放，再为加载进来的笔迹建轻量引用。
                StrokeInitialHistory.Clear();

                // 添加新墨迹：分块添加，逐块给每笔补事件订阅（不克隆点集）。
                phase.Restart();
                for (int offset = 0; offset < total; offset += StbPerfAddChunk)
                {
                    int count = Math.Min(StbPerfAddChunk, total - offset);
                    StrokeCollection part = new StrokeCollection();
                    for (int i = 0; i < count; i++)
                    {
                        Stroke stroke = loaded[offset + i];
                        StbPerfHookStroke(stroke, this);
                        part.Add(stroke);
                    }
                    inkCanvas.Strokes.Add(part);
                    int done = offset + count;
                    StbPerfShowBusy(string.Format("正在应用墨迹… {0} / {1}（{2}%）", done, total, (int)(done * 100.0 / Math.Max(1, total))));
                    StbPerfPump();
                }
                addMs = phase.ElapsedMilliseconds;

                // 历史与旧笔迹引用全部释放，避免大文件加载后内存里还挂着两份。
                inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;
                handlerDetached = false;
                timeMachine.ClearStrokeHistory();
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                // 保留 Key（DrawingAttributesHistoryFlag 的 Guid 键是后续属性变更回调要用的），只清空引用
                foreach (KeyValuePair<Guid, List<Stroke>> pair in DrawingAttributesHistoryFlag)
                {
                    pair.Value.Clear();
                }

                totalMs = totalWatch.ElapsedMilliseconds;
                string message = string.Format("笔记已加载：{0} 笔，{1:0.0} MB，用时 {2:0.0} 秒", total, bytes / 1048576.0, totalMs / 1000.0);
                StbPerfHideBusy();
                if (!isStartup)
                {
                    ShowNotification(message);
                }
                LogHelper.NewLog(string.Format("StbPerf: load '{0}' strokes={1} bytes={2} parseMs={3} clearMs={4} addMs={5} totalMs={6}", path, total, bytes, parseMs, clearMs, addMs, totalMs));
            }
            catch (Exception ex)
            {
                StbPerfHideBusy();
                LogHelper.NewLog(string.Format("StbPerf: load failed '{0}' {1}", path, ex.Message));
                ShowNotification("墨迹打开失败：" + ex.Message);
            }
            finally
            {
                if (handlerDetached)
                {
                    inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;
                }
                inkCanvas.IsHitTestVisible = previousHitTest;
                Mouse.OverrideCursor = previousCursor;
                _stbPerfLoadingInk = false;
            }
        }
    }
}
