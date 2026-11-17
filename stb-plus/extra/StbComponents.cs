using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace InkCanvasPlus
{
    /// <summary>STB 组件基类：定义一个可独立开窗的小工具（时钟、倒计时、随机点名…）。</summary>
    public abstract class StbComponentBase
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Symbol { get; }
        public abstract string Description { get; }

        /// <summary>组件内容。每次开窗调用一次，返回的元素直接放进组件窗口。</summary>
        public abstract FrameworkElement BuildView();

        /// <summary>窗口关闭时释放定时器等资源。</summary>
        public virtual void OnClose() { }
    }

    /// <summary>时钟组件：大号时间 + 日期，每秒刷新。</summary>
    public sealed class StbClockComponent : StbComponentBase
    {
        private DispatcherTimer _timer;

        public override string Id { get { return "stb.clock"; } }
        public override string Title { get { return "时钟"; } }
        public override string Symbol { get { return "Contact"; } }
        public override string Description { get { return "课堂看时间，不占讲台"; } }

        public override FrameworkElement BuildView()
        {
            var root = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            var time = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                FontSize = 40,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF))
            };
            var date = new TextBlock
            {
                Text = DateTime.Now.ToString("yyyy年M月d日 dddd"),
                FontSize = 13,
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromArgb(0xB3, 0xE8, 0xEB, 0xEF))
            };
            root.Children.Add(time);
            root.Children.Add(date);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                DateTime now = DateTime.Now;
                time.Text = now.ToString("HH:mm:ss");
                date.Text = now.ToString("yyyy年M月d日 dddd");
            };
            _timer.Start();
            return root;
        }

        public override void OnClose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }
    }

    /// <summary>倒计时组件：预设 1/3/5 分钟，可开始暂停、重置。</summary>
    public sealed class StbCountdownComponent : StbComponentBase
    {
        private DispatcherTimer _timer;
        private int _remain = 180;
        private bool _running = false;
        private TextBlock _time;
        private Button _btnStart;

        public override string Id { get { return "stb.countdown"; } }
        public override string Title { get { return "倒计时"; } }
        public override string Symbol { get { return "Play"; } }
        public override string Description { get { return "限时练习、随堂测倒计时"; } }

        public override FrameworkElement BuildView()
        {
            var root = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

            _time = new TextBlock
            {
                Text = "03:00",
                FontSize = 40,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF))
            };
            root.Children.Add(_time);

            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0) };
            _btnStart = MakeButton("开始");
            _btnStart.Click += (s, e) => Toggle();
            var btnReset = MakeButton("重置");
            btnReset.Click += (s, e) =>
            {
                Stop();
                _remain = 180;
                UpdateText();
            };
            row.Children.Add(_btnStart);
            row.Children.Add(btnReset);
            root.Children.Add(row);

            var presets = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            presets.Children.Add(MakePreset("1 分钟", 60));
            presets.Children.Add(MakePreset("3 分钟", 180));
            presets.Children.Add(MakePreset("5 分钟", 300));
            root.Children.Add(presets);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                if (_remain > 0)
                {
                    _remain -= 1;
                    UpdateText();
                }
                if (_remain <= 0)
                {
                    Stop();
                }
            };

            return root;
        }

        private Button MakeButton(string text)
        {
            var b = new Button
            {
                Content = text,
                MinWidth = 64,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF))
            };
            return b;
        }

        private Button MakePreset(string text, int seconds)
        {
            var b = MakeButton(text);
            b.Click += (s, e) =>
            {
                Stop();
                _remain = seconds;
                UpdateText();
            };
            return b;
        }

        private void Toggle()
        {
            if (_running)
            {
                Stop();
            }
            else
            {
                if (_remain <= 0)
                {
                    _remain = 180;
                }
                _running = true;
                if (_btnStart != null)
                {
                    _btnStart.Content = "暂停";
                }
                _timer.Start();
            }
        }

        private void Stop()
        {
            _running = false;
            if (_timer != null)
            {
                _timer.Stop();
            }
            if (_btnStart != null)
            {
                _btnStart.Content = "开始";
            }
        }

        private void UpdateText()
        {
            if (_time == null)
            {
                return;
            }
            int min = _remain / 60;
            int sec = _remain % 60;
            _time.Text = min.ToString("00") + ":" + sec.ToString("00");
        }

        public override void OnClose()
        {
            Stop();
            _timer = null;
        }
    }

    /// <summary>随机点名组件：1–上限之间随机取号，上限用 +/- 调整。</summary>
    public sealed class StbRandomPickerComponent : StbComponentBase
    {
        private int _max = 40;
        private TextBlock _number;
        private TextBlock _range;
        private Random _rand = new Random();

        public override string Id { get { return "stb.random"; } }
        public override string Title { get { return "随机点名"; } }
        public override string Symbol { get { return "People"; } }
        public override string Description { get { return "按学号随机抽一位同学"; } }

        public override FrameworkElement BuildView()
        {
            var root = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

            _number = new TextBlock
            {
                Text = "?",
                FontSize = 52,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF))
            };
            root.Children.Add(_number);

            _range = new TextBlock
            {
                Text = "1 – 40",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromArgb(0xB3, 0xE8, 0xEB, 0xEF))
            };
            root.Children.Add(_range);

            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0) };
            var btnPick = new Button
            {
                Content = "抽一个",
                MinWidth = 90,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF))
            };
            btnPick.Click += (s, e) => { _number.Text = (_rand.Next(1, _max + 1)).ToString(); };
            var btnDown = new Button { Content = "人数 -", Width = 64, Height = 30, Margin = new Thickness(0, 0, 8, 0), Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF)) };
            btnDown.Click += (s, e) => { ChangeMax(-5); };
            var btnUp = new Button { Content = "人数 +", Width = 64, Height = 30, Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF)) };
            btnUp.Click += (s, e) => { ChangeMax(5); };
            row.Children.Add(btnPick);
            row.Children.Add(btnDown);
            row.Children.Add(btnUp);
            root.Children.Add(row);

            return root;
        }

        private void ChangeMax(int delta)
        {
            _max = _max + delta;
            if (_max < 5)
            {
                _max = 5;
            }
            if (_max > 100)
            {
                _max = 100;
            }
            if (_range != null)
            {
                _range.Text = "1 – " + _max.ToString();
            }
        }

        public override void OnClose()
        {
            _number = null;
            _range = null;
        }
    }
}
