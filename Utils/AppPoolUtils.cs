using website.updater.Models;

namespace website.updater.Utils
{
    public static class AppPoolUtils
    {
        /// <summary>
        ///  檢查應用程式集區名稱
        /// </summary>
        /// <param name="appSettings">應用程式相關設定</param>
        /// <param name="appPoolName">應用程式應用程式集區名稱</param>
        /// <returns></returns>
        public static (string destinationPath, string backupPath) CheckAppPoolName(AppSettings appSettings, string appPoolName)
        {
            var applyInfo = appSettings.AppSetting.FirstOrDefault(r => r.AppName == appPoolName);

            return applyInfo == null ? (string.Empty, string.Empty) : (applyInfo.DirectoryName, applyInfo.BackupPath);
        }

        /// <summary>
        /// 備份目錄
        /// </summary>
        /// <param name="sourcePath">來源路徑</param>
        /// <param name="backupBasePath">備份基礎路徑</param>
        /// <param name="appName">應用程式名稱</param>
        public static void BackupDirectory(string sourcePath, string backupBasePath, string appName)
        {
            DirectoryUtils.BackupDirectory(sourcePath, backupBasePath, appName);
        }
    }
}
