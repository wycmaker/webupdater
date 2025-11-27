using System.IO.Compression;

namespace website.updater.Utils
{
    public class ZipUtils
    {
        /// <summary>
        /// 解壓縮ZIP檔案
        /// </summary>
        /// <param name="fileStream">檔案串流</param>
        /// <param name="destinationPath">目標資料夾</param>
        public void ExtractZipFile(Stream fileStream, string destinationPath)
        {
            using (var arc = new ZipArchive(fileStream))
            {
                foreach (var entry in arc.Entries)
                {
                    string path = Path.Combine(destinationPath, entry.FullName);

                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

                    if (!string.IsNullOrEmpty(entry.Name)) // 忽略資料夾
                    {
                        entry.ExtractToFile(path, overwrite: true);
                        Console.WriteLine($"Extracted: {entry.FullName}");
                    }
                }
            }
        }
    }
}
