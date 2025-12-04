namespace website.updater.Models
{
    public class AppSetting
    {
        /// <summary>
        /// 應用程式集區名稱
        /// </summary>
        public string AppName { get; set; } = string.Empty;

        /// <summary>
        /// 資料夾名稱
        /// </summary>
        public string DirectoryName { get; set; } = string.Empty;

        /// <summary>
        /// 備份路徑
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;

        /// <summary>
        /// 排除項目
        /// </summary>
        public List<string> ExcludeItem { get; set; } = new();
    }
}
