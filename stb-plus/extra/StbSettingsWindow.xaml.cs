using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// STB 设置页：左侧分类导航 + 右侧卡片（对齐 SCHOOL_TOOLS_PDF 的设置页版式）。
    /// 直接读写 MainWindow.Settings，任何改动即时生效并自动落盘。
    /// </summary>
    public partial class StbSettingsWindow : Window
    {
        private static StbSettingsWindow _instance;
        private bool _loading = true;
        private Button[] _navButtons;

        private const string HomeUrl = "https://github.com/me13354091017/SCHOOL_TOOLS_BOARD";
        private const string SiteUrl = "https://me13354091017.github.io/school_tools/";

        public static void ShowFor(Window owner)
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }
            var w = new StbSettingsWindow();
            if (owner != null && owner.IsLoaded)
            {
                w.Owner = owner;
            }
            _instance = w;
            w.Closed += (s, e) => { _instance = null; };
            w.Show();
            w.Activate();
        }

        public StbSettingsWindow()
        {
            InitializeComponent();
            _navButtons = new[] { BtnNav0, BtnNav1, BtnNav2, BtnNav3, BtnNav4, BtnNav5, BtnNav6 };
            TextVersion.Text = "版本 " + Assembly.GetExecutingAssembly().GetName().Version;
            LoadFromSettings();
            SelectSection(0);
            _loading = false;
        }

        private static Settings S => MainWindow.Settings;

        private void Save()
        {
            if (_loading)
            {
                return;
            }
            MainWindow.SaveSettingsToFile();
        }

        private void LoadFromSettings()
        {
            _loading = true;
            try
            {
                SliderInkWidth.Value = S.Canvas.InkWidth;
                TextInkWidth.Text = S.Canvas.InkWidth.ToString("0.0");
                SwShowCursor.IsOn = S.Canvas.IsShowCursor;
                SwHideStrokeWhenSelecting.IsOn = S.Canvas.HideStrokeWhenSelecting;

                int theme = S.Appearance.Theme;
                foreach (object item in ComboTheme.Items)
                {
                    ComboBoxItem ci = item as ComboBoxItem;
                    if (ci != null && ci.Tag != null && ci.Tag.ToString() == theme.ToString())
                    {
                        ComboTheme.SelectedItem = ci;
                        break;
                    }
                }
                if (ComboTheme.SelectedItem == null)
                {
                    ComboTheme.SelectedIndex = 0;
                }
                SwAutoCollapseFloatBar.IsOn = S.Appearance.IsAutoCollapseFloatBar;
                SwFloatBarRight.IsOn = S.Appearance.IsFloatBarShowOnRight;
                SwShowExit.IsOn = S.Appearance.IsShowExitButton;

                SwAutoHideCanvas.IsOn = S.Startup.IsAutoHideCanvas;
                SwAutoEnterFinger.IsOn = S.Startup.IsAutoEnterModeFinger;
                SwTwoFingerZoom.IsOn = S.Gesture.IsEnableTwoFingerZoom;
                SwTwoFingerTranslate.IsOn = S.Gesture.IsEnableTwoFingerTranslate;
                SwTwoFingerRotate.IsOn = S.Gesture.IsEnableTwoFingerRotation;
                SwRunAtStartup.IsOn = StartupShortcutExists();

                SwPptSupport.IsOn = S.PowerPointSettings.PowerPointSupport;
                SwPptNav.IsOn = S.PowerPointSettings.IsShowPPTNavigation;
                SwPptAutoSaveStrokes.IsOn = S.PowerPointSettings.IsAutoSaveStrokesInPowerPoint;
                SwPptNotifyPrev.IsOn = S.PowerPointSettings.IsNotifyPreviousPage;
                SwPptWps.IsOn = S.PowerPointSettings.IsSupportWPS;

                SwShotDateFolder.IsOn = S.Automation.IsSaveScreenshotsInDateFolders;
                SwAutoSaveAtShot.IsOn = S.Automation.IsAutoSaveStrokesAtScreenshot;
                SwAutoSaveAtClear.IsOn = S.Automation.IsAutoSaveStrokesAtClear;
                SwAutoClearExit.IsOn = S.Automation.IsAutoClearWhenExitingWritingMode;

                SwSpecialScreen.IsOn = S.Advanced.IsSpecialScreen;
                SliderTouchMultiplier.Value = S.Advanced.TouchMultiplier;
                TextTouchMultiplier.Text = S.Advanced.TouchMultiplier.ToString("0.00");
                SwQuadIR.IsOn = S.Advanced.IsQuadIR;
                SwLog.IsOn = S.Advanced.IsLogEnabled;
                SwDisableEdgeGesture.IsOn = S.Advanced.DisableEdgeGesture;
            }
            finally
            {
                _loading = false;
            }
        }

        private static bool StartupShortcutExists()
        {
            try
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "InkCanvas.lnk");
                return File.Exists(p);
            }
            catch
            {
                return false;
            }
        }

        private void SelectSection(int index)
        {
            for (int i = 0; i < _navButtons.Length; i++)
            {
                bool on = i == index;
                _navButtons[i].Background = on ? new SolidColorBrush(Color.FromRgb(0xE3, 0xEC, 0xE6)) : Brushes.Transparent;
                _navButtons[i].Foreground = on ? new SolidColorBrush(Color.FromRgb(0x14, 0x52, 0x3B)) : new SolidColorBrush(Color.FromRgb(0x3F, 0x3C, 0x34));
                _navButtons[i].FontWeight = on ? FontWeights.SemiBold : FontWeights.Normal;
            }
            Panel0.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            Panel1.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            Panel2.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
            Panel3.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
            Panel4.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;
            Panel5.Visibility = index == 5 ? Visibility.Visible : Visibility.Collapsed;
            Panel6.Visibility = index == 6 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null || b.Tag == null)
            {
                return;
            }
            int idx;
            if (int.TryParse(b.Tag.ToString(), out idx))
            {
                SelectSection(idx);
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            try
            {
                DragMove();
            }
            catch
            {
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 墨迹书写
        private void SliderInkWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextInkWidth == null)
            {
                return;
            }
            S.Canvas.InkWidth = SliderInkWidth.Value;
            TextInkWidth.Text = SliderInkWidth.Value.ToString("0.0");
            Save();
        }

        private void SwShowCursor_Toggled(object sender, RoutedEventArgs e)
        {
            S.Canvas.IsShowCursor = SwShowCursor.IsOn;
            Save();
        }

        private void SwHideStrokeWhenSelecting_Toggled(object sender, RoutedEventArgs e)
        {
            S.Canvas.HideStrokeWhenSelecting = SwHideStrokeWhenSelecting.IsOn;
            Save();
        }

        // 外观主题
        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading)
            {
                return;
            }
            ComboBoxItem ci = ComboTheme.SelectedItem as ComboBoxItem;
            if (ci == null || ci.Tag == null)
            {
                return;
            }
            int idx;
            if (!int.TryParse(ci.Tag.ToString(), out idx))
            {
                return;
            }
            S.Appearance.Theme = idx;
            Save();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            try
            {
                MainWindow mw = Owner as MainWindow;
                if (mw != null)
                {
                    mw.ApplyStbTheme();
                }
            }
            catch
            {
            }
        }

        private void SwAutoCollapseFloatBar_Toggled(object sender, RoutedEventArgs e)
        {
            S.Appearance.IsAutoCollapseFloatBar = SwAutoCollapseFloatBar.IsOn;
            Save();
        }

        private void SwFloatBarRight_Toggled(object sender, RoutedEventArgs e)
        {
            S.Appearance.IsFloatBarShowOnRight = SwFloatBarRight.IsOn;
            Save();
        }

        private void SwShowExit_Toggled(object sender, RoutedEventArgs e)
        {
            S.Appearance.IsShowExitButton = SwShowExit.IsOn;
            Save();
        }

        // 画板行为
        private void SwAutoHideCanvas_Toggled(object sender, RoutedEventArgs e)
        {
            S.Startup.IsAutoHideCanvas = SwAutoHideCanvas.IsOn;
            Save();
        }

        private void SwAutoEnterFinger_Toggled(object sender, RoutedEventArgs e)
        {
            S.Startup.IsAutoEnterModeFinger = SwAutoEnterFinger.IsOn;
            Save();
        }

        private void SwTwoFingerZoom_Toggled(object sender, RoutedEventArgs e)
        {
            S.Gesture.IsEnableTwoFingerZoom = SwTwoFingerZoom.IsOn;
            Save();
        }

        private void SwTwoFingerTranslate_Toggled(object sender, RoutedEventArgs e)
        {
            S.Gesture.IsEnableTwoFingerTranslate = SwTwoFingerTranslate.IsOn;
            Save();
        }

        private void SwTwoFingerRotate_Toggled(object sender, RoutedEventArgs e)
        {
            S.Gesture.IsEnableTwoFingerRotation = SwTwoFingerRotate.IsOn;
            Save();
        }

        private void SwRunAtStartup_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }
            if (SwRunAtStartup.IsOn)
            {
                MainWindow.StartAutomaticallyCreate("InkCanvas");
            }
            else
            {
                MainWindow.StartAutomaticallyDel("InkCanvas");
            }
        }

        // PPT 批注
        private void SwPptSupport_Toggled(object sender, RoutedEventArgs e)
        {
            S.PowerPointSettings.PowerPointSupport = SwPptSupport.IsOn;
            Save();
        }

        private void SwPptNav_Toggled(object sender, RoutedEventArgs e)
        {
            S.PowerPointSettings.IsShowPPTNavigation = SwPptNav.IsOn;
            Save();
        }

        private void SwPptAutoSaveStrokes_Toggled(object sender, RoutedEventArgs e)
        {
            S.PowerPointSettings.IsAutoSaveStrokesInPowerPoint = SwPptAutoSaveStrokes.IsOn;
            Save();
        }

        private void SwPptNotifyPrev_Toggled(object sender, RoutedEventArgs e)
        {
            S.PowerPointSettings.IsNotifyPreviousPage = SwPptNotifyPrev.IsOn;
            Save();
        }

        private void SwPptWps_Toggled(object sender, RoutedEventArgs e)
        {
            S.PowerPointSettings.IsSupportWPS = SwPptWps.IsOn;
            Save();
        }

        // 数据存储
        private void SwShotDateFolder_Toggled(object sender, RoutedEventArgs e)
        {
            S.Automation.IsSaveScreenshotsInDateFolders = SwShotDateFolder.IsOn;
            Save();
        }

        private void SwAutoSaveAtShot_Toggled(object sender, RoutedEventArgs e)
        {
            S.Automation.IsAutoSaveStrokesAtScreenshot = SwAutoSaveAtShot.IsOn;
            Save();
        }

        private void SwAutoSaveAtClear_Toggled(object sender, RoutedEventArgs e)
        {
            S.Automation.IsAutoSaveStrokesAtClear = SwAutoSaveAtClear.IsOn;
            Save();
        }

        private void SwAutoClearExit_Toggled(object sender, RoutedEventArgs e)
        {
            S.Automation.IsAutoClearWhenExitingWritingMode = SwAutoClearExit.IsOn;
            Save();
        }

        // 高级
        private void SwSpecialScreen_Toggled(object sender, RoutedEventArgs e)
        {
            S.Advanced.IsSpecialScreen = SwSpecialScreen.IsOn;
            Save();
        }

        private void SliderTouchMultiplier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextTouchMultiplier == null)
            {
                return;
            }
            S.Advanced.TouchMultiplier = SliderTouchMultiplier.Value;
            TextTouchMultiplier.Text = SliderTouchMultiplier.Value.ToString("0.00");
            Save();
        }

        private void SwQuadIR_Toggled(object sender, RoutedEventArgs e)
        {
            S.Advanced.IsQuadIR = SwQuadIR.IsOn;
            Save();
        }

        private void SwLog_Toggled(object sender, RoutedEventArgs e)
        {
            S.Advanced.IsLogEnabled = SwLog.IsOn;
            Save();
        }

        private void SwDisableEdgeGesture_Toggled(object sender, RoutedEventArgs e)
        {
            S.Advanced.DisableEdgeGesture = SwDisableEdgeGesture.IsOn;
            Save();
        }

        // 关于
        private void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow mw = Owner as MainWindow;
                if (mw != null)
                {
                    mw.CheckForUpdate();
                }
            }
            catch
            {
            }
        }

        private void BtnOpenHome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(HomeUrl);
            }
            catch
            {
            }
        }

        private void BtnViewLicense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(SiteUrl);
            }
            catch
            {
            }
        }
    }
}
