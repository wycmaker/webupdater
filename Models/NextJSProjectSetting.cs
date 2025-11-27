namespace website.updater.Models
{
    public class NextJSProjectSetting
    {
        /// <summary>
        /// 專案名稱
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// PM2 程序 ID
        /// </summary>
        public string Pm2Id { get; set; } = string.Empty;

        /// <summary>
        /// 專案資料夾路徑
        /// </summary>
        public string DirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// 備份路徑
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;
    }
}

