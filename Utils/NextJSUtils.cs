using website.updater.Models;

namespace website.updater.Utils
{
    public static class NextJSUtils
    {
        /// <summary>
        /// 檢查 NextJS 專案名稱並返回設定資訊
        /// </summary>
        /// <param name="appSettings">應用程式相關設定</param>
        /// <param name="projectName">專案名稱</param>
        /// <returns>NextJSProjectSetting 物件，如果找不到則返回 null</returns>
        public static NextJSProjectSetting? GetNextJSProjectSetting(AppSettings appSettings, string projectName)
        {
            return appSettings.NextJSProjects.FirstOrDefault(p => p.ProjectName == projectName);
        }

        /// <summary>
        /// 備份 NextJS 專案
        /// </summary>
        /// <param name="sourcePath">來源路徑</param>
        /// <param name="backupBasePath">備份基礎路徑</param>
        /// <param name="projectName">專案名稱</param>
        /// <param name="excludeItems">要排除的項目列表</param>
        public static void BackupNextJSProject(string sourcePath, string backupBasePath, string projectName, List<string>? excludeItems = null)
        {
            // 創建新列表，避免修改傳入的列表
            var finalExcludeItems = excludeItems == null 
                ? new List<string> { "node_modules" } 
                : new List<string>(excludeItems) { "node_modules" };
            
            DirectoryUtils.BackupDirectory(sourcePath, backupBasePath, projectName, finalExcludeItems);
        }

        /// <summary>
        /// 清除專案目錄
        /// </summary>
        /// <param name="targetPath">目標路徑</param>
        /// <param name="excludeItems">要排除的項目列表</param>
        public static void ClearProjectDirectory(string targetPath, List<string>? excludeItems = null)
        {
            // 創建新列表，避免修改傳入的列表
            var finalExcludeItems = excludeItems == null 
                ? new List<string> { "node_modules" } 
                : new List<string>(excludeItems) { "node_modules" };
            
            DirectoryUtils.ClearDirectory(targetPath, finalExcludeItems);
        }
    }
}

