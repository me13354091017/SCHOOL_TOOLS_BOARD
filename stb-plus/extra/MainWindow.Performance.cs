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
    // 1.0.1.58：10MB+ 大笔记加载。
    // 真机基线（34,000 笔 / 10.7MB）：解析 1.5s；Strokes.Clear 约 15s；逐块 Add 更慢（每块都会触发全量重绘）。
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
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
                {
                    StbLoadInkFile(path, true);
                }));
            }
        }

        private static string StbPerfGetStartupInkPath()
        {
            string[] args = App.StartArgs;
            if (args == null) { return null; }
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
            if (_stbPerfBusyLayer != null) { _stbPerfBusyLayer.Visibility = Visibility.Collapsed; }
        }

        private StrokeCollection StbPerfReadInk(string path, out long bytes)
        {
            byte[] data = File.ReadAllBytes(path);
            bytes = data.Length;
            using (MemoryStream ms = new MemoryStream(data))
            {
                StrokeCollection strokes = new StrokeCollection(ms);
                if (strokes.Count > 0) { return strokes; }
            }
            using (MemoryStream ms = new MemoryStream(data))
            {
                return new StrokeCollection(ms);
            }
        }

        private static void StbPerfHookStroke(Stroke stroke, MainWindow owner)
        {
            owner.StrokeInitialHistory[stroke] = stroke.StylusPoints;
            stroke.StylusPointsChanged += owner.Stroke_StylusPointsChanged;
            stroke.StylusPointsReplaced += owner.Stroke_StylusPointsReplaced;
            stroke.DrawingAttributesChanged += owner.Stroke_DrawingAttributesChanged;
        }

        private static bool StbPerfHasArg(string name)
        {
            string[] args = App.StartArgs;
            if (args == null) { return false; }
            foreach (string a in args)
            {
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        private void StbPerfSaveTest(string sourcePath)
        {
            try
            {
                StylusPointCollection pts = new StylusPointCollection();
                for (int i = 0; i <= 120; i++)
                {
                    pts.Add(new StylusPoint(700 + i * 6, 500 + (int)(40 * Math.Sin(i / 120.0 * Math.PI))));
                }
                inkCanvas.Strokes.Add(new Stroke(pts));

                string dir = Path.Combine(Path.GetTempPath(), "stb_perf");
                Directory.CreateDirectory(dir);
                string outPath = Path.Combine(dir, "saved_" + Path.GetFileNameWithoutExtension(sourcePath) + ".icstk");
                Stopwatch sw = Stopwatch.StartNew();
                using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                {
                    inkCanvas.Strokes.Save(fs);
                }
                sw.Stop();
                LogHelper.NewLog(string.Format("StbPerf: savetest strokes={0} bytes={1} ms={2} path='{3}'",
                    inkCanvas.Strokes.Count, new FileInfo(outPath).Length, sw.ElapsedMilliseconds, outPath));
            }
            catch (Exception ex)
            {
                LogHelper.NewLog("StbPerf: savetest failed " + ex.Message);
            }
        }

        private void StbLoadInkFile(string path, bool isStartup)
        {
            if (_stbPerfLoadingInk) { return; }
            _stbPerfLoadingInk = true;
            Stopwatch totalWatch = Stopwatch.StartNew();
            long parseMs = 0, applyMs = 0, hookMs = 0, clearMs = 0, bytes = 0;
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

                StbPerfShowBusy(string.Format("正在应用墨迹… {0} 笔（原有 {1} 笔）", total, oldCount));
                StbPerfPump();

                inkCanvas.Strokes.StrokesChanged -= StrokesOnStrokesChanged;
                handlerDetached = true;
                StrokeInitialHistory.Clear();
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (KeyValuePair<Guid, List<Stroke>> pair in DrawingAttributesHistoryFlag) { pair.Value.Clear(); }
                phase.Restart();
                if (loaded != null)
                {
                    foreach (Stroke stroke in loaded) { StbPerfHookStroke(stroke, this); }
                }
                hookMs = phase.ElapsedMilliseconds;

                phase.Restart();
                inkCanvas.Strokes = loaded;
                applyMs = phase.ElapsedMilliseconds;

                inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;
                handlerDetached = false;
                timeMachine.ClearStrokeHistory();

                if (StbPerfHasArg("-savetest")) { StbPerfSaveTest(path); }

                long totalMs = totalWatch.ElapsedMilliseconds;
                string message = string.Format("笔记已加载：{0} 笔，{1:0.0} MB，用时 {2:0.0} 秒", total, bytes / 1048576.0, totalMs / 1000.0);
                StbPerfHideBusy();
                if (!isStartup) { ShowNotification(message); }
                LogHelper.NewLog(string.Format("StbPerf: load '{0}' strokes={1} bytes={2} parseMs={3} hookMs={4} clearMs={5} addMs={6} totalMs={7} old={8}", path, total, bytes, parseMs, hookMs, clearMs, applyMs, totalMs, oldCount));
            }
            catch (Exception ex)
            {
                StbPerfHideBusy();
                LogHelper.NewLog(string.Format("StbPerf: load failed '{0}' {1}", path, ex.Message));
                ShowNotification("墨迹打开失败：" + ex.Message);
            }
            finally
            {
                if (handlerDetached) { inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged; }
                inkCanvas.IsHitTestVisible = previousHitTest;
                Mouse.OverrideCursor = previousCursor;
                _stbPerfLoadingInk = false;
            }
        }
    }
}
