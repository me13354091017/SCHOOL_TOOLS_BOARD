using System;
using System.Windows;
using System.Windows.Controls;

namespace InkCanvasPlus
{
    /// <summary>
    /// 设置页「橡皮擦大小」档位按钮（partial 文件，不改动 StbSettingsWindow.xaml.cs）：
    /// 档位沿用上游 Settings.Canvas.EraserSize（-2…4），换算成实际像素直径展示，并立即生效 + 落盘。
    /// </summary>
    public partial class StbSettingsWindow
    {
        private static readonly int[] StbEraserLevels = new int[] { -2, -1, 0, 1, 2, 3, 4 };
        private static readonly double[] StbEraserCoeffs = new double[] { 0.5, 0.8, 1.0, 1.6, 2.0, 2.5, 3.6 };

        private void TextEraserSize_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateEraserSizeUi();
        }

        private void BtnEraserSizeMinus_Click(object sender, RoutedEventArgs e)
        {
            ShiftEraserLevel(-1);
        }

        private void BtnEraserSizePlus_Click(object sender, RoutedEventArgs e)
        {
            ShiftEraserLevel(1);
        }

        private void ShiftEraserLevel(int delta)
        {
            int idx = Array.IndexOf(StbEraserLevels, S.Canvas.EraserSize);
            if (idx < 0)
            {
                idx = 2;
            }
            idx = Math.Max(0, Math.Min(StbEraserLevels.Length - 1, idx + delta));
            S.Canvas.EraserSize = StbEraserLevels[idx];
            UpdateEraserSizeUi();
            ApplyEraserSize();
            Save();
        }

        private void UpdateEraserSizeUi()
        {
            if (TextEraserSize == null)
            {
                return;
            }
            int idx = Array.IndexOf(StbEraserLevels, S.Canvas.EraserSize);
            double k = idx >= 0 ? StbEraserCoeffs[idx] : 1.0;
            TextEraserSize.Text = "直径 " + ((int)Math.Round(50d * k)).ToString() + " px";
        }

        private void ApplyEraserSize()
        {
            MainWindow mw = Owner as MainWindow;
            if (mw == null && Application.Current != null)
            {
                mw = Application.Current.MainWindow as MainWindow;
            }
            if (mw != null)
            {
                mw.StbRefreshEraserShape();
            }
        }
    }
}
