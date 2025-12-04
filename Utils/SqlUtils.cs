using System.Data;
using Microsoft.Data.SqlClient;

namespace website.updater.Utils
{
    public static class SqlUtils
    {
        /// <summary>
        /// 執行 SQL 查詢命令（SELECT）
        /// </summary>
        /// <param name="connectionString">資料庫連接字串</param>
        /// <param name="sqlCommand">SQL 命令</param>
        /// <param name="commandTimeout">命令逾時時間（秒）</param>
        /// <returns>查詢結果（DataTable）</returns>
        public static DataTable ExecuteQuery(string connectionString, string sqlCommand, int commandTimeout = 30)
        {
            var dataTable = new DataTable();

            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(sqlCommand, connection)
                {
                    CommandTimeout = commandTimeout,
                    CommandType = CommandType.Text
                };

                connection.Open();
                using var adapter = new SqlDataAdapter(command);
                adapter.Fill(dataTable);
            }
            catch (Exception ex)
            {
                throw new Exception($"執行 SQL 查詢失敗: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// 執行 SQL 非查詢命令（INSERT、UPDATE、DELETE、CREATE、ALTER 等）
        /// </summary>
        /// <param name="connectionString">資料庫連接字串</param>
        /// <param name="sqlCommand">SQL 命令</param>
        /// <param name="commandTimeout">命令逾時時間（秒）</param>
        /// <returns>受影響的列數</returns>
        public static int ExecuteNonQuery(string connectionString, string sqlCommand, int commandTimeout = 30)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(sqlCommand, connection)
                {
                    CommandTimeout = commandTimeout,
                    CommandType = CommandType.Text
                };

                connection.Open();
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"執行 SQL 命令失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 執行 SQL 命令（自動判斷為查詢或非查詢）
        /// </summary>
        /// <param name="connectionString">資料庫連接字串</param>
        /// <param name="sqlCommand">SQL 命令</param>
        /// <param name="isQuery">是否為查詢操作</param>
        /// <param name="commandTimeout">命令逾時時間（秒）</param>
        /// <returns>執行結果</returns>
        public static object Execute(string connectionString, string sqlCommand, bool isQuery, int commandTimeout = 30)
        {
            if (isQuery)
            {
                var dataTable = ExecuteQuery(connectionString, sqlCommand, commandTimeout);
                // 將 DataTable 轉換為易於序列化的格式
                return ConvertDataTableToDictionary(dataTable);
            }
            else
            {
                var affectedRows = ExecuteNonQuery(connectionString, sqlCommand, commandTimeout);
                return new Dictionary<string, object> { { "AffectedRows", affectedRows } };
            }
        }

        /// <summary>
        /// 將 DataTable 轉換為字典列表（易於 JSON 序列化）
        /// </summary>
        public static List<Dictionary<string, object?>> ConvertDataTableToDictionary(DataTable dataTable)
        {
            var result = new List<Dictionary<string, object?>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var rowDict = new Dictionary<string, object?>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    var value = row[column];
                    // 處理 DBNull 值
                    rowDict[column.ColumnName] = value == DBNull.Value ? null : value;
                }
                result.Add(rowDict);
            }

            return result;
        }

        /// <summary>
        /// 驗證 SQL 命令是否安全（基本檢查）
        /// </summary>
        /// <param name="sqlCommand">SQL 命令</param>
        /// <returns>是否安全</returns>
        public static bool IsSqlCommandSafe(string sqlCommand)
        {
            if (string.IsNullOrWhiteSpace(sqlCommand))
            {
                return false;
            }

            // 檢查是否包含危險的關鍵字（可根據需求調整）
            var dangerousKeywords = new[] { "DROP DATABASE", "TRUNCATE TABLE", "EXEC xp_cmdshell", "EXEC sp_configure", "DROP TABLE" };
            var upperCommand = sqlCommand.ToUpperInvariant();

            foreach (var keyword in dangerousKeywords)
            {
                if (upperCommand.Contains(keyword))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

