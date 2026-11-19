using System;
using System.Collections.Generic;
using System.IO;

namespace InkCanvasPlus
{
    /// <summary>浮动栏条目：id、名称、字形图标、说明。</summary>
    public sealed class StbBarItemInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Glyph { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 浮动栏组件配置（%AppData%\SchoolToolsBoard\floatbar.txt）：
    ///   enabled=1       使用自定义浮动栏（组件系统接管，隐藏内置工具组）
    ///   size=18         图标尺寸
    ///   text=1          是否图标+文字
    ///   items=id1,id2…  条目顺序
    /// </summary>
    public static class StbBarRegistry
    {
        private static bool _loaded = false;
        private static bool _enabled = false;
        private static double _size = 18d;
        private static bool _showText = true;
        private static readonly List<string> _items = new List<string>();

        public static string ConfigPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SchoolToolsBoard");
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                catch
                {
                }
                return Path.Combine(dir, "floatbar.txt");
            }
        }

        public static List<StbBarItemInfo> All
        {
            get
            {
                List<StbBarItemInfo> list = new List<StbBarItemInfo>();
                Add(list, "stb.bar.miniboard", "板中板", "▣", "浮层白板，可与主画板数据互通。");
                Add(list, "stb.bar.components", "组件", "◈", "打开组件系统，配置主界面与浮动栏。");
                Add(list, "stb.bar.settings", "设置", "⚙", "打开 STB 设置页。");
                Add(list, "stb.bar.pen", "画笔", "✎", "切到书写模式。");
                Add(list, "stb.bar.highlighter", "荧光笔", "▮", "切到荧光笔书写。");
                Add(list, "stb.bar.eraser", "橡皮", "▨", "切到橡皮（整笔擦除）。");
                Add(list, "stb.bar.select", "选择", "▭", "切到套索选择。");
                Add(list, "stb.bar.clear", "清屏", "✕", "清空当前画布的所有笔迹。");
                Add(list, "stb.bar.fullscreen", "全屏", "⛶", "切换窗口全屏。");
                Add(list, "stb.bar.exit", "退出", "⏻", "退出 SchoolToolsBoard。");
                return list;
            }
        }

        private static void Add(List<StbBarItemInfo> list, string id, string title, string glyph, string desc)
        {
            StbBarItemInfo info = new StbBarItemInfo();
            info.Id = id;
            info.Title = title;
            info.Glyph = glyph;
            info.Description = desc;
            list.Add(info);
        }

        public static StbBarItemInfo Find(string id)
        {
            foreach (StbBarItemInfo info in All)
            {
                if (info.Id == id)
                {
                    return info;
                }
            }
            return null;
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }
            _loaded = true;
            _items.Clear();
            try
            {
                string path = ConfigPath;
                if (!File.Exists(path))
                {
                    _enabled = false;
                    _size = 18d;
                    _showText = true;
                    return;
                }
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    int i = line.IndexOf('=');
                    if (i <= 0)
                    {
                        continue;
                    }
                    string key = line.Substring(0, i).Trim();
                    string val = line.Substring(i + 1).Trim();
                    if (key == "enabled")
                    {
                        _enabled = val == "1";
                    }
                    else if (key == "size")
                    {
                        double d;
                        if (double.TryParse(val, out d))
                        {
                            _size = Math.Max(12d, Math.Min(30d, d));
                        }
                    }
                    else if (key == "text")
                    {
                        _showText = val == "1";
                    }
                    else if (key == "items")
                    {
                        foreach (string part in val.Split(','))
                        {
                            string id = part.Trim();
                            if (id.Length > 0 && Find(id) != null && !_items.Contains(id))
                            {
                                _items.Add(id);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public static bool Enabled
        {
            get
            {
                EnsureLoaded();
                return _enabled;
            }
            set
            {
                EnsureLoaded();
                _enabled = value;
                Save();
            }
        }

        public static double Size
        {
            get
            {
                EnsureLoaded();
                return _size;
            }
            set
            {
                EnsureLoaded();
                _size = Math.Max(12d, Math.Min(30d, value));
                Save();
            }
        }

        public static bool ShowText
        {
            get
            {
                EnsureLoaded();
                return _showText;
            }
            set
            {
                EnsureLoaded();
                _showText = value;
                Save();
            }
        }

        public static List<string> LoadItems()
        {
            EnsureLoaded();
            return new List<string>(_items);
        }

        public static void SaveItems(IEnumerable<string> ids)
        {
            EnsureLoaded();
            _items.Clear();
            foreach (string id in ids)
            {
                if (id != null && Find(id) != null && !_items.Contains(id))
                {
                    _items.Add(id);
                }
            }
            Save();
        }

        public static void Save()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("enabled=" + (_enabled ? "1" : "0"));
                lines.Add("size=" + _size.ToString("0"));
                lines.Add("text=" + (_showText ? "1" : "0"));
                lines.Add("items=" + string.Join(",", _items.ToArray()));
                File.WriteAllLines(ConfigPath, lines.ToArray());
            }
            catch
            {
            }
        }
    }
}
