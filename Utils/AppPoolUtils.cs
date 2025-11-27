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
            try
            {
                // 創建帶時間戳記的備份目錄名稱
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupBasePath, $"{appName}_{timestamp}");

                // 確保備份基礎目錄存在
                Directory.CreateDirectory(backupBasePath);

                // 複製整個目錄
                CopyDirectory(sourcePath, backupPath);

                Console.WriteLine($"備份完成: {sourcePath} -> {backupPath}");

                // 清理超過1個禮拜的備份檔案
                CleanOldBackups(backupBasePath, appName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"備份失敗: {ex.Message}");
                throw new Exception($"備份失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理超過1個禮拜的備份檔案
        /// </summary>
        /// <param name="backupBasePath">備份基礎路徑</param>
        /// <param name="appName">應用程式名稱</param>
        private static void CleanOldBackups(string backupBasePath, string appName)
        {
            try
            {
                if (!Directory.Exists(backupBasePath))
                {
                    return;
                }

                // 計算1個禮拜前的日期
                DateTime oneWeekAgo = DateTime.Now.AddDays(-7);
                string prefix = $"{appName}_";

                // 取得所有備份目錄
                var backupDirectories = Directory.GetDirectories(backupBasePath)
                    .Where(dir => Path.GetFileName(dir).StartsWith(prefix))
                    .ToList();

                foreach (var backupDir in backupDirectories)
                {
                    try
                    {
                        string dirName = Path.GetFileName(backupDir);

                        // 從目錄名稱中提取時間戳記（格式：{appName}_{yyyyMMdd_HHmmss}）
                        if (dirName.Length > prefix.Length)
                        {
                            string timestampStr = dirName[prefix.Length..];

                            // 嘗試解析時間戳記（yyyyMMdd_HHmmss）
                            if (timestampStr.Length >= 8)
                            {
                                string dateStr = timestampStr[..8]; // yyyyMMdd

                                if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime backupDate))
                                {
                                    // 如果備份日期超過1個禮拜，刪除該備份目錄
                                    if (backupDate < oneWeekAgo)
                                    {
                                        DeleteDirectoryRecursively(backupDir);
                                        Console.WriteLine($"已刪除舊備份: {backupDir}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"清理備份時發生錯誤 ({backupDir}): {ex.Message}");
                        // 繼續處理其他備份目錄，不中斷流程
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理舊備份失敗: {ex.Message}");
                // 不拋出異常，避免影響主要備份流程
            }
        }

        /// <summary>
        /// 遞迴刪除目錄及其所有內容（強制刪除，包含唯讀檔案）
        /// </summary>
        /// <param name="targetDir">目標目錄</param>
        private static void DeleteDirectoryRecursively(string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                return;
            }

            try
            {
                DirectoryInfo dir = new(targetDir);

                // 移除所有檔案的唯讀屬性並刪除
                foreach (FileInfo file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (file.Exists)
                        {
                            file.Attributes = FileAttributes.Normal; // 移除唯讀屬性
                            file.Delete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"刪除檔案時發生錯誤 ({file.FullName}): {ex.Message}");
                        // 繼續處理其他檔案
                    }
                }

                // 刪除所有子目錄
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    try
                    {
                        DeleteDirectoryRecursively(subDir.FullName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"刪除子目錄時發生錯誤 ({subDir.FullName}): {ex.Message}");
                        // 繼續處理其他目錄
                    }
                }

                // 最後刪除目錄本身
                dir.Attributes = FileAttributes.Normal; // 移除唯讀屬性
                dir.Delete();
            }
            catch (Exception ex)
            {
                // 如果遞迴刪除失敗，嘗試使用 Directory.Delete
                try
                {
                    Directory.Delete(targetDir, true);
                }
                catch
                {
                    throw new Exception($"無法刪除目錄: {targetDir}. 錯誤: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 複製目錄及其所有內容
        /// </summary>
        /// <param name="sourceDir">來源目錄</param>
        /// <param name="destDir">目標目錄</param>
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            DirectoryInfo dir = new(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"來源目錄不存在: {sourceDir}");
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // 創建目標目錄
            Directory.CreateDirectory(destDir);

            // 複製所有檔案
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite: true);
            }

            // 遞迴複製所有子目錄
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }
}
