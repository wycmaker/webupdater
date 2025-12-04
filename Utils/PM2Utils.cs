using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace website.updater.Utils
{
    [SupportedOSPlatform("windows")]
    public static class PM2Utils
    {
        private static string _pm2Path = string.Empty;

        public static void Initialize(string pm2Path)
        {
            // 初始化邏輯（如果有需要）
            _pm2Path = pm2Path;
        }

        /// <summary>
        /// 檢查是否以系統管理員權限執行
        /// </summary>
        [SupportedOSPlatform("windows")]
        private static bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 執行 PM2 命令（需要系統管理員權限）
        /// 注意：應用程式本身必須以系統管理員權限執行，否則 PM2 命令可能失敗
        /// </summary>
        /// <param name="command">PM2 命令（例如：stop 0, start 0, save）</param>
        /// <returns>命令執行結果</returns>
        private static (bool success, string output, string error) ExecutePM2Command(string command)
        {
            try
            {
                // 檢查是否以管理員權限運行
                if (!IsRunningAsAdministrator())
                {
                    return (false, string.Empty, "應用程式必須以系統管理員權限執行才能使用 PM2 命令");
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {_pm2Path} {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return (false, string.Empty, "無法啟動程序");
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit(30000); // 最多等待 30 秒

                bool success = process.ExitCode == 0;
                return (success, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// 停止 PM2 程序
        /// </summary>
        /// <param name="pm2Id">PM2 程序 ID</param>
        /// <returns>執行結果</returns>
        public static (bool success, string message) StopProcess(string pm2Id)
        {
            var (success, _, error) = ExecutePM2Command($"stop {pm2Id}");

            if (success)
            {
                return (true, $"PM2 程序 {pm2Id} 已停止");
            }
            else
            {
                return (false, $"停止 PM2 程序 {pm2Id} 失敗: {error}");
            }
        }

        /// <summary>
        /// 啟動 PM2 程序
        /// </summary>
        /// <param name="pm2Id">PM2 程序 ID</param>
        /// <returns>執行結果</returns>
        public static (bool success, string message) StartProcess(string pm2Id)
        {
            var (success, _, error) = ExecutePM2Command($"start {pm2Id}");

            if (success)
            {
                return (true, $"PM2 程序 {pm2Id} 已啟動");
            }
            else
            {
                return (false, $"啟動 PM2 程序 {pm2Id} 失敗: {error}");
            }
        }

        /// <summary>
        /// 儲存 PM2 設定
        /// </summary>
        /// <returns>執行結果</returns>
        public static (bool success, string message) Save()
        {
            var (success, _, error) = ExecutePM2Command("save");

            if (success)
            {
                return (true, "PM2 設定已儲存");
            }
            else
            {
                return (false, $"儲存 PM2 設定失敗: {error}");
            }
        }
    }
}

