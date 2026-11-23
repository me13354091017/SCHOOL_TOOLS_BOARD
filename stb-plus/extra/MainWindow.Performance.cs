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
    // 1.0.1.51：10MB+ 大笔记加载。
    // 真机基线（34,000 笔 / 10.7MB）：解析 1.5s；Strokes.Clear 约 15s；逐块 Add 更慢（每块都会触发全量重绘）。
    // 最省时的做法是把整份 StrokeCollection 一次性赋给 InkCanvas（实测约 4.8s），
    // 期间用遮罩提示进度、禁止误触，并在赋值前后接管 StrokesChanged 订阅与历史字典。
    public partial class MainWindow
    {
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

        /// <summary>让 Dispatcher 处理一轮输入/渲染，避免进度提示还没画出来就开始长任务。</summary>
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
            long parseMs = 0;
            long applyMs = 0;
            long bytes = 0;
            int total = 0;
            int oldCount = inkCanvas.Strokes.Count;

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

                // 一次性替换整份集合：WPF 只建一次视觉树；分块 Add 每块都会全量重绘，实测反而更慢。
                StbPerfShowBusy(string.Format("正在应用墨迹… {0} 笔（原有 {1} 笔，约 5 秒）", total, oldCount));
                StbPerfPump();

                // 先摘掉旧集合的处理器；随后为加载进来的每笔补订阅（不克隆点集）。
                inkCanvas.Strokes.StrokesChanged -= StrokesOnStrokesChanged;
                handlerDetached = true;
                StrokeInitialHistory.Clear();
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (KeyValuePair<Guid, List<Stroke>> pair in DrawingAttributesHistoryFlag)
                {
                    pair.Value.Clear();
                }
                if (loaded != null)
                {
                    foreach (Stroke stroke in loaded)
                    {
                        StbPerfHookStroke(stroke, this);
                    }
                }

                phase.Restart();
                inkCanvas.Strokes = loaded;
                applyMs = phase.ElapsedMilliseconds;

                inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;
                handlerDetached = false;
                timeMachine.ClearStrokeHistory();

                long totalMs = totalWatch.ElapsedMilliseconds;
                string message = string.Format("笔记已加载：{0} 笔，{1:0.0} MB，用时 {2:0.0} 秒", total, bytes / 1048576.0, totalMs / 1000.0);
                StbPerfHideBusy();
                if (!isStartup)
                {
                    ShowNotification(message);
                }
                LogHelper.NewLog(string.Format("StbPerf: load '{0}' strokes={1} bytes={2} parseMs={3} applyMs={4} totalMs={5} old={6}", path, total, bytes, parseMs, applyMs, totalMs, oldCount));
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
