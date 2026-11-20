using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

/// <summary>
/// SchoolToolsBoard 启动器（1.0.1.35 起的安装版布局）：
/// {app}\SchoolToolsBoard.exe   ← 本启动器
/// {app}\app-x.y.z.w-0\        ← 实际程序与 dll
/// {app}\data\                  ← 配置 / 日志 / 存档
/// 启动时挑选版本号最大的 app-* 目录，转到其中的 SchoolToolsBoard.exe。
/// </summary>
internal static class StbLauncher
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string data = Path.Combine(root, "data");
            if (!Directory.Exists(data))
            {
                Directory.CreateDirectory(data);
            }

            string best = null;
            Version bestVer = new Version(0, 0, 0, 0);
            foreach (string dir in Directory.GetDirectories(root, "app-*"))
            {
                Version v = ParseVersion(Path.GetFileName(dir));
                if (best == null || v > bestVer)
                {
                    best = dir;
                    bestVer = v;
                }
            }

            if (best == null)
            {
                MessageBox.Show("没有找到 app-* 程序目录，请重新安装 SchoolToolsBoard。",
                    "SchoolToolsBoard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string exe = Path.Combine(best, "SchoolToolsBoard.exe");
            if (!File.Exists(exe))
            {
                MessageBox.Show("程序文件缺失：" + exe, "SchoolToolsBoard",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ProcessStartInfo psi = new ProcessStartInfo(exe);
            psi.WorkingDirectory = best;
            psi.UseShellExecute = false;
            if (args != null && args.Length > 0)
            {
                psi.Arguments = JoinArgs(args);
            }
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show("启动失败：" + ex.Message, "SchoolToolsBoard",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>app-1.0.1.35-0 → 1.0.1.35。</summary>
    private static Version ParseVersion(string name)
    {
        string s = name;
        if (s.StartsWith("app-", StringComparison.OrdinalIgnoreCase))
        {
            s = s.Substring(4);
        }
        int dash = s.LastIndexOf('-');
        if (dash > 0)
        {
            s = s.Substring(0, dash);
        }
        Version v;
        if (Version.TryParse(s, out v))
        {
            return v;
        }
        return new Version(0, 0, 0, 0);
    }

    private static string JoinArgs(string[] args)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string a in args)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            if (a.IndexOf(' ') >= 0)
            {
                sb.Append('"').Append(a).Append('"');
            }
            else
            {
                sb.Append(a);
            }
        }
        return sb.ToString();
    }
}
