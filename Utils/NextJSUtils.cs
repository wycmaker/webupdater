namespace website.updater.Utils
{
    public static class NextJSUtils
    {
        /// <summary>
        /// 備份 NextJS 專案（排除 node_modules）
        /// </summary>
        /// <param name="sourcePath">來源路徑</param>
        /// <param name="backupBasePath">備份基礎路徑</param>
        /// <param name="projectName">專案名稱</param>
        public static void BackupNextJSProject(string sourcePath, string backupBasePath, string projectName)
        {
            DirectoryUtils.BackupDirectory(sourcePath, backupBasePath, projectName, "node_modules");
        }

        /// <summary>
        /// 清除專案目錄（保留 node_modules）
        /// </summary>
        /// <param name="targetPath">目標路徑</param>
        public static void ClearProjectDirectory(string targetPath)
        {
            DirectoryUtils.ClearDirectory(targetPath);
        }
    }
}

