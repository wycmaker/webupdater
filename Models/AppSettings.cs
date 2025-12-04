namespace website.updater.Models
{
    public class AppSettings
    {
        /// <summary>
        /// 應用程式集區設定
        /// </summary>
        public List<AppSetting> AppSetting { get; set; } = [];

        /// <summary>
        /// NextJS 專案設定
        /// </summary>
        public List<NextJSProjectSetting> NextJSProjects { get; set; } = [];

        /// <summary>
        /// pm2 可執行檔路徑
        /// </summary>
        public string Pm2Path { get; set; } = "pm2";

        /// <summary>
        /// 資料庫連接字串設定
        /// </summary>
        public Dictionary<string, string> ConnectionStrings { get; set; } = new();

        /// <summary>
        /// 預設資料庫連接字串名稱
        /// </summary>
        public string DefaultConnectionStringName { get; set; } = "Default";
    }
}
