using System.IO.Compression;
using System.Text;

namespace website.updater.Utils
{
    public static class ZipUtils
    {
        /// <summary>
        /// 解壓縮ZIP檔案，自動嘗試多種編碼以處理不同來源的ZIP檔案
        /// </summary>
        /// <param name="fileStream">檔案串流</param>
        /// <param name="destinationPath">目標資料夾</param>
        public static void ExtractZipFile(Stream fileStream, string destinationPath)
        {
            // 註冊編碼提供者以支援更多編碼格式（如 Big5、GBK 等）
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // 定義要嘗試的編碼列表（按優先順序）
            var encodings = new[]
            {
                Encoding.UTF8,                    // UTF-8（現代標準，最常用）
                Encoding.Default,                 // 系統預設編碼（繁體中文 Windows 通常是 Big5）
                Encoding.GetEncoding(950),       // Big5（繁體中文）
                Encoding.GetEncoding(936),       // GBK（簡體中文）
            };

            // 如果 Stream 不可搜尋，需要將內容讀取到記憶體以便重複嘗試
            Stream streamToUse = fileStream;
            bool needDispose = false;
            
            if (!fileStream.CanSeek)
            {
                var memoryStream = new MemoryStream();
                fileStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                streamToUse = memoryStream;
                needDispose = true;
            }

            try
            {
                Exception? lastException = null;
                
                // 嘗試每種編碼
                foreach (var encoding in encodings)
                {
                    try
                    {
                        // 重置 Stream 位置
                        streamToUse.Position = 0;
                        
                        using var arc = new ZipArchive(streamToUse, ZipArchiveMode.Read, leaveOpen: true, encoding);
                        
                        // 檢查是否有項目，並驗證編碼是否正確
                        if (arc.Entries.Count > 0)
                        {
                            // 檢查前幾個檔名是否包含明顯的亂碼
                            bool hasInvalidChars = false;
                            int checkCount = arc.Entries.Count;
                            
                            for (int i = 0; i < checkCount; i++)
                            {
                                var entry = arc.Entries[i];
                                if (ContainsInvalidChars(entry.FullName))
                                {
                                    hasInvalidChars = true;
                                    break;
                                }
                            }
                            
                            if (hasInvalidChars)
                            {
                                continue; // 如果包含無效字元，嘗試下一個編碼
                            }
                        }
                        
                        // 編碼看起來正確，開始解壓縮
                        streamToUse.Position = 0;
                        using var finalArc = new ZipArchive(streamToUse, ZipArchiveMode.Read, leaveOpen: false, encoding);
                        foreach (var entry in finalArc.Entries)
                        {
                            string path = Path.Combine(destinationPath, entry.FullName);

                            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

                            if (!string.IsNullOrEmpty(entry.Name)) // 忽略資料夾
                            {
                                entry.ExtractToFile(path, overwrite: true);
                            }
                        }
                        
                        // 成功解壓縮，結束
                        return;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        // 繼續嘗試下一個編碼
                        continue;
                    }
                }
                
                // 所有編碼都失敗，拋出最後一個例外
                throw lastException ?? new InvalidOperationException("無法使用任何編碼解壓縮 ZIP 檔案");
            }
            finally
            {
                if (needDispose)
                {
                    streamToUse.Dispose();
                }
            }
        }
        
        /// <summary>
        /// 檢查字串是否包含無效的控制字元（可能是亂碼）
        /// </summary>
        private static bool ContainsInvalidChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
                
            foreach (char c in text)
            {
                // 檢查是否為控制字元（除了常見的空白字元）
                if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t')
                {
                    return true;
                }
                
                // 檢查是否為替換字元（U+FFFD），這通常是解碼錯誤的標記
                if (c == '\uFFFD')
                {
                    return true;
                }
            }
            return false;
        }
    }
}
