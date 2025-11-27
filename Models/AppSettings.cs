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
    }
}
