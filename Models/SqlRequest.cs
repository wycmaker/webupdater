namespace website.updater.Models
{
    /// <summary>
    /// SQL 執行請求模型
    /// </summary>
    public class SqlRequest
    {
        /// <summary>
        /// SQL 命令（SELECT、INSERT、UPDATE、DELETE、CREATE、ALTER 等）
        /// </summary>
        public string SqlCommand { get; set; } = string.Empty;

        /// <summary>
        /// 是否為查詢操作（SELECT），預設為 false
        /// 如果為 true，會返回查詢結果；如果為 false，會返回受影響的列數
        /// </summary>
        public bool IsQuery { get; set; } = false;

        /// <summary>
        /// 命令逾時時間（秒），預設為 30 秒
        /// </summary>
        public int CommandTimeout { get; set; } = 30;
    }
}

