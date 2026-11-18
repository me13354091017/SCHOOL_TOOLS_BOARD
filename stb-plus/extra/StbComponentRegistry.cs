using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace InkCanvasPlus
{
    public sealed class StbComponentInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public Func<StbComponentBase> Create { get; set; }
    }

    public sealed class StbInfoComponent : StbComponentBase
    {
        private readonly string _id;
        private readonly string _title;
        private readonly string _symbol;
        private readonly string _desc;
        private readonly string _staticText;
        private readonly Func<string> _dynamic;
        private DispatcherTimer _timer;

        public StbInfoComponent(string id, string title, string symbol, string desc, string staticText, Func<string> dynamic)
        {
            _id = id; _title = title; _symbol = symbol; _desc = desc; _staticText = staticText; _dynamic = dynamic;
        }

        public override string Id { get { return _id; } }
        public override string Title { get { return _title; } }
        public override string Symbol { get { return _symbol; } }
        public override string Description { get { return _desc; } }

        public override FrameworkElement BuildView()
        {
            StackPanel root = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            TextBlock body = new TextBlock
            {
                Text = _dynamic != null ? _dynamic() : _staticText,
                FontSize = 22,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF))
            };
            root.Children.Add(body);
            if (_dynamic != null)
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += (s, e) => { try { body.Text = _dynamic(); } catch { } };
                _timer.Start();
            }
            return root;
        }

        public override void OnClose()
        {
            if (_timer != null) { _timer.Stop(); _timer = null; }
        }
    }

    public static class StbComponentRegistry
    {
        private static readonly DateTime StartedAt = DateTime.Now;

        public static string ConfigPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SchoolToolsBoard");
                try { if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); } } catch { }
                return Path.Combine(dir, "components.txt");
            }
        }

        public static List<StbComponentInfo> All
        {
            get
            {
                List<StbComponentInfo> list = new List<StbComponentInfo>();

                list.Add(new StbComponentInfo { Id = "stb.clock", Title = "时钟", Symbol = "Contact", Description = "显示现在的时间，支持精确到秒。", Create = () => new StbClockComponent() });
                list.Add(new StbComponentInfo { Id = "stb.countdown", Title = "倒计时", Symbol = "Play", Description = "显示距离某一天的倒计时。", Create = () => new StbCountdownComponent() });
                list.Add(new StbComponentInfo { Id = "stb.random", Title = "随机点名", Symbol = "People", Description = "按学号随机抽一位同学。", Create = () => new StbRandomPickerComponent() });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.date", Title = "日期", Symbol = "Calendar", Description = "显示今天的日期和星期。",
                    Create = () => new StbInfoComponent("stb.date", "日期", "Calendar", "显示今天的日期和星期。", null, () => DateTime.Now.ToString("yyyy年M月d日 dddd"))
                });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.week", Title = "今日周数（年）", Symbol = "Calendar", Description = "显示本年第几周。",
                    Create = () => new StbInfoComponent("stb.week", "今日周数（年）", "Calendar", "显示本年第几周。", null, () => "第 " + System.Globalization.ISOWeek.GetWeekOfYear(DateTime.Now) + " 周")
                });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.text", Title = "文本", Symbol = "Edit", Description = "显示自定义文本。",
                    Create = () => new StbInfoComponent("stb.text", "文本", "Edit", "显示自定义文本。", "课堂提示\n请保持安静", null)
                });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.divider", Title = "分割线", Symbol = "Remove", Description = "显示一个分割线，视觉上对组件进行分组。",
                    Create = () => new StbInfoComponent("stb.divider", "分割线", "Remove", "显示一个分割线，视觉上对组件进行分组。", "────────────", null)
                });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.timetable", Title = "课程表", Symbol = "OpenFile", Description = "显示当前的课程表信息。",
                    Create = () => new StbInfoComponent("stb.timetable", "课程表", "OpenFile", "显示当前的课程表信息。", "今日课程\n语文 数学 英语\n物理 化学 自习", null)
                });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.weather", Title = "天气简报", Symbol = "Refresh", Description = "显示当前的天气概况和气象预警。",
                    Create = () => new StbInfoComponent("stb.weather", "天气简报", "Refresh", "显示当前的天气概况和气象预警。", "晴 12℃\n东北风 3 级", null)
                });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.stopwatch", Title = "正向计时器（ATI）", Symbol = "Play", Description = "从打开组件开始正向计时。",
                    Create = () => new StbInfoComponent("stb.stopwatch", "正向计时器（ATI）", "Play", "从打开组件开始正向计时。", null, () => (DateTime.Now - StartedAt).ToString("hh\\:mm\\:ss"))
                });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.cycle", Title = "周期性倒计时（ATI）", Symbol = "Refresh", Description = "按 45 分钟一节课循环倒计时。",
                    Create = () => new StbInfoComponent("stb.cycle", "周期性倒计时（ATI）", "Refresh", "按 45 分钟一节课循环倒计时。", null, () =>
                    {
                        double period = 45d * 60d;
                        double elapsed = (DateTime.Now.TimeOfDay.TotalSeconds) % period;
                        double remain = period - elapsed;
                        return "距下段 " + TimeSpan.FromSeconds(remain).ToString("mm\\:ss");
                    })
                });

                list.Add(new StbComponentInfo
                {
                    Id = "stb.multi", Title = "多倒计时（ATI）", Symbol = "Play", Description = "同时显示多个重要时间点。",
                    Create = () => new StbInfoComponent("stb.multi", "多倒计时（ATI）", "Play", "同时显示多个重要时间点。", null, () =>
                    {
                        DateTime now = DateTime.Now;
                        DateTime schoolEnd = now.Date.AddHours(17).AddMinutes(30);
                        DateTime tomorrow = now.Date.AddDays(1).AddHours(8);
                        return "放学 " + Span(schoolEnd - now) + "\n明天上课 " + Span(tomorrow - now);
                    })
                });

                foreach (string kind in new string[] { "分组容器", "堆叠容器", "滚动容器", "轮播容器" })
                {
                    string k = kind;
                    list.Add(new StbComponentInfo
                    {
                        Id = "stb.container." + k,
                        Title = k,
                        Symbol = "Add",
                        Description = k == "分组容器" ? "将多个组件组合到一个组件中显示。" : (k == "堆叠容器" ? "将多个容器堆叠显示。" : (k == "滚动容器" ? "滚动显示组件内的内容。" : "轮播多个组件。")),
                        Create = () => new StbInfoComponent("stb.container." + k, k, "Add", "容器组件（简化版）。", k + "\n（可放入多个组件）", null)
                    });
                }

                return list;
            }
        }

        private static string Span(TimeSpan span)
        {
            if (span.TotalSeconds < 0) { return "已到"; }
            if (span.TotalHours >= 1) { return ((int)span.TotalHours) + "时" + span.Minutes + "分"; }
            return span.Minutes + "分";
        }

        public static StbComponentInfo Find(string id)
        {
            foreach (StbComponentInfo info in All) { if (info.Id == id) { return info; } }
            return null;
        }

        public static List<string> LoadActive()
        {
            List<string> ids = new List<string>();
            try
            {
                string path = ConfigPath;
                if (!File.Exists(path)) { ids.Add("stb.clock"); return ids; }
                foreach (string line in File.ReadAllLines(path))
                {
                    string t = line.Trim();
                    if (t.Length > 0 && Find(t) != null && !ids.Contains(t)) { ids.Add(t); }
                }
            }
            catch { }
            return ids;
        }

        public static void SaveActive(IEnumerable<string> ids)
        {
            try { File.WriteAllLines(ConfigPath, new List<string>(ids).ToArray()); } catch { }
        }
    }
}
