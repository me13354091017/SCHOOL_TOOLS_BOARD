using System;
using System.Diagnostics;

namespace InkCanvasPlus
{
    /// <summary>
    /// 性能相关的小工具（partial 文件，不改动上游代码）：
    /// 上游 PPT 轮询每 300ms 会调两次 Process.GetProcessesByName，既昂贵又会泄漏进程句柄；
    /// 这里换成带 900ms 缓存的判断，用完立即 Dispose，CPU 与句柄占用都明显下降。
    /// </summary>
    public partial class MainWindow
    {
        private DateTime _stbOfficeCheckAt = DateTime.MinValue;
        private bool _stbOfficeRunning = false;

        private bool StbOfficeProcessRunning()
        {
            DateTime now = DateTime.Now;
            if ((now - _stbOfficeCheckAt).TotalMilliseconds < 900d)
            {
                return _stbOfficeRunning;
            }

            _stbOfficeCheckAt = now;
            _stbOfficeRunning = StbAnyProcessNamed("POWERPNT") || StbAnyProcessNamed("wpp");
            return _stbOfficeRunning;
        }

        private static bool StbAnyProcessNamed(string name)
        {
            Process[] found = null;
            try
            {
                found = Process.GetProcessesByName(name);
                return found.Length > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (found != null)
                {
                    foreach (Process p in found)
                    {
                        try
                        {
                            p.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
    }
}
