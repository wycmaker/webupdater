using Microsoft.Web.Administration;
using website.updater.Models;

namespace website.updater.Utils
{
    public static class AppPoolUtils
    {
        /// <summary>
        ///  檢查應用程式集區名稱並返回設定資訊
        /// </summary>
        /// <param name="appSettings">應用程式相關設定</param>
        /// <param name="appPoolName">應用程式應用程式集區名稱</param>
        /// <returns>AppSetting 物件，如果找不到則返回 null</returns>
        public static AppSetting? GetAppSetting(AppSettings appSettings, string appPoolName)
        {
            return appSettings.AppSetting.FirstOrDefault(r => r.AppName == appPoolName);
        }

        /// <summary>
        /// 備份目錄
        /// </summary>
        /// <param name="sourcePath">來源路徑</param>
        /// <param name="backupBasePath">備份基礎路徑</param>
        /// <param name="appName">應用程式名稱</param>
        /// <param name="excludeItems">要排除的項目列表</param>
        public static void BackupDirectory(string sourcePath, string backupBasePath, string appName, List<string>? excludeItems = null)
        {
            DirectoryUtils.BackupDirectory(sourcePath, backupBasePath, appName, excludeItems);
        }

        /// <summary>
        /// 清除專案目錄
        /// </summary>
        /// <param name="targetPath">目標路徑</param>
        /// <param name="excludeItems">要排除的項目列表</param>
        public static void ClearProjectDirectory(string targetPath, List<string>? excludeItems = null)
        {
            DirectoryUtils.ClearDirectory(targetPath, excludeItems);
        }

        /// <summary>
        /// 依應用程式集區名稱取得應用程式集區物件
        /// </summary>
        /// <param name="appPoolName">應用程式集區名稱</param>
        /// <returns></returns>
        public static ApplicationPool? GetAppPoolByName(string appPoolName)
        {
            using ServerManager serverManager = new();
            return serverManager.ApplicationPools[appPoolName];
        }

        /// <summary>
        /// 執行應用程式集區操作（不進行驗證，用於內部調用）
        /// </summary>
        /// <param name="appPool">應用程式集區物件</param>
        /// <param name="action">要執行的操作</param>
        public static void ExecuteAppPoolAction(ApplicationPool appPool, Action<ApplicationPool> action)
        {
            action(appPool);
            Thread.Sleep(1000);
        }
    }
}
