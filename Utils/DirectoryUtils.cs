namespace website.updater.Utils
{
    public static class DirectoryUtils
    {
        /// <summary>
        /// 檢查項目是否在排除列表中
        /// </summary>
        /// <param name="itemName">項目名稱</param>
        /// <param name="excludeItems">排除項目列表</param>
        /// <returns>如果項目應該被排除則返回 true</returns>
        private static bool IsExcluded(string itemName, List<string>? excludeItems)
        {
            return excludeItems != null && excludeItems.Any(exclude =>
                itemName.Equals(exclude, StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>
        /// 備份目錄到指定位置
        /// </summary>
        /// <param name="sourcePath">來源路徑</param>
        /// <param name="backupBasePath">備份基礎路徑</param>
        /// <param name="backupName">備份名稱（用於生成備份目錄名稱）</param>
        /// <param name="excludeItems">要排除的項目列表（檔案或資料夾名稱，不區分大小寫）</param>
        public static void BackupDirectory(string sourcePath, string backupBasePath, string backupName, List<string>? excludeItems = null)
        {
            try
            {
                // 創建帶時間戳記的備份目錄名稱
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupBasePath, $"{backupName}_{timestamp}");

                // 確保備份基礎目錄存在
                Directory.CreateDirectory(backupBasePath);

                // 複製目錄（可選排除特定項目）
                CopyDirectoryWithExclusion(sourcePath, backupPath, excludeItems);

                // 清理超過1個禮拜的備份檔案
                CleanOldBackups(backupBasePath, backupName);
            }
            catch (Exception ex)
            {
                throw new Exception($"備份失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理超過指定天數的備份檔案
        /// </summary>
        /// <param name="backupBasePath">備份基礎路徑</param>
        /// <param name="backupName">備份名稱前綴</param>
        /// <param name="retentionDays">保留天數（預設7天）</param>
        public static void CleanOldBackups(string backupBasePath, string backupName, int retentionDays = 7)
        {
            try
            {
                if (!Directory.Exists(backupBasePath))
                {
                    return;
                }

                // 計算保留日期
                DateTime retentionDate = DateTime.Now.AddDays(-retentionDays);
                string prefix = $"{backupName}_";

                // 取得所有備份目錄
                var backupDirectories = Directory.GetDirectories(backupBasePath)
                    .Where(dir => Path.GetFileName(dir).StartsWith(prefix))
                    .ToList();

                foreach (var backupDir in backupDirectories)
                {
                    try
                    {
                        string dirName = Path.GetFileName(backupDir);

                        // 從目錄名稱中提取時間戳記（格式：{backupName}_{yyyyMMdd_HHmmss}）
                        if (dirName.Length > prefix.Length)
                        {
                            string timestampStr = dirName[prefix.Length..];

                            // 嘗試解析時間戳記（yyyyMMdd_HHmmss）
                            if (timestampStr.Length >= 8)
                            {
                                string dateStr = timestampStr[..8]; // yyyyMMdd

                                if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime backupDate))
                                {
                                    // 如果備份日期超過保留期限，刪除該備份目錄
                                    if (backupDate < retentionDate)
                                    {
                                        DeleteDirectoryRecursively(backupDir);
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
        /// 複製目錄（可選排除指定的檔案或資料夾）
        /// </summary>
        /// <param name="sourceDir">來源目錄</param>
        /// <param name="destDir">目標目錄</param>
        /// <param name="excludeItems">要排除的項目列表（檔案或資料夾名稱，不區分大小寫），為 null 或空列表時不排除任何項目</param>
        public static void CopyDirectoryWithExclusion(string sourceDir, string destDir, List<string>? excludeItems)
        {
            DirectoryInfo dir = new(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"來源目錄不存在: {sourceDir}");
            }

            // 創建目標目錄
            Directory.CreateDirectory(destDir);

            // 複製所有檔案（排除指定檔案）
            foreach (FileInfo file in dir.GetFiles())
            {
                if (IsExcluded(file.Name, excludeItems))
                {
                    continue;
                }

                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite: true);
            }

            // 遞迴複製所有子目錄（排除指定目錄）
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                if (IsExcluded(subDir.Name, excludeItems))
                {
                    continue;
                }

                string newDestinationDir = Path.Combine(destDir, subDir.Name);
                CopyDirectoryWithExclusion(subDir.FullName, newDestinationDir, excludeItems);
            }
        }

        /// <summary>
        /// 遞迴刪除目錄及其所有內容（強制刪除，包含唯讀檔案）
        /// </summary>
        /// <param name="targetDir">目標目錄</param>
        public static void DeleteDirectoryRecursively(string targetDir)
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
        /// 清除目錄內容（可選保留特定檔案或資料夾）
        /// </summary>
        /// <param name="targetPath">目標路徑</param>
        /// <param name="excludeItems">要排除的項目列表（檔案或資料夾名稱，不區分大小寫）</param>
        public static void ClearDirectory(string targetPath, List<string>? excludeItems = null)
        {
            if (!Directory.Exists(targetPath))
            {
                return;
            }

            try
            {
                DirectoryInfo dir = new(targetPath);

                // 刪除所有檔案（排除指定檔案）
                foreach (FileInfo file in dir.GetFiles())
                {
                    try
                    {
                        if (IsExcluded(file.Name, excludeItems))
                        {
                            continue;
                        }

                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"刪除檔案時發生錯誤 ({file.FullName}): {ex.Message}");
                    }
                }

                // 刪除所有子目錄（保留指定目錄）
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    try
                    {
                        if (IsExcluded(subDir.Name, excludeItems))
                        {
                            continue;
                        }

                        DeleteDirectoryRecursively(subDir.FullName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"刪除子目錄時發生錯誤 ({subDir.FullName}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清除目錄失敗: {ex.Message}");
                throw;
            }
        }
    }
}

