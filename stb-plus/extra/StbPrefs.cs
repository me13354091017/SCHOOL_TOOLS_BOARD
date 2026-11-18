using System;
using System.IO;

namespace InkCanvasPlus
{
    /// <summary>
    /// STB 自己的小型偏好开关（存 %AppData%\SchoolToolsBoard\prefs.txt，一行 "键=值"）。
    /// 不往上游 Settings JSON 里塞字段，避免反序列化踩坑。
    /// </summary>
    internal static class StbPrefs
    {
        private static string Dir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SchoolToolsBoard");
            }
        }

        private static string FilePath
        {
            get
            {
                return Path.Combine(Dir, "prefs.txt");
            }
        }

        /// <summary>橡皮擦完抬笔后自动回到书写；默认开。</summary>
        internal static bool AutoPenBack
        {
            get
            {
                return ReadFlag("autoPenBack", true);
            }
            set
            {
                WriteFlag("autoPenBack", value);
            }
        }

        private static bool ReadFlag(string key, bool fallback)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return fallback;
                }
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0)
                    {
                        continue;
                    }
                    if (line.Substring(0, i).Trim() != key)
                    {
                        continue;
                    }
                    string v = line.Substring(i + 1).Trim();
                    return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "开";
                }
            }
            catch
            {
            }
            return fallback;
        }

        private static void WriteFlag(string key, bool value)
        {
            try
            {
                Directory.CreateDirectory(Dir);

                string[] lines = File.Exists(FilePath) ? File.ReadAllLines(FilePath) : new string[0];
                var sb = new System.Text.StringBuilder();
                bool written = false;
                foreach (string line in lines)
                {
                    int i = line.IndexOf('=');
                    if (i > 0 && line.Substring(0, i).Trim() == key)
                    {
                        sb.AppendLine(key + "=" + (value ? "1" : "0"));
                        written = true;
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                if (!written)
                {
                    sb.AppendLine(key + "=" + (value ? "1" : "0"));
                }
                File.WriteAllText(FilePath, sb.ToString());
            }
            catch
            {
            }
        }
    }
}
