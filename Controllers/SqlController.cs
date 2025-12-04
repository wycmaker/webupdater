using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using website.updater.Filters;
using website.updater.Models;
using website.updater.Utils;

namespace website.updater.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SqlController(IOptions<AppSettings> options) : ControllerBase
    {
        /// <summary>
        /// 執行 SQL 命令（查詢、更新、刪除、DDL 操作等）
        /// </summary>
        /// <param name="connectionStringName">連接字串名稱（選填，預設使用 Default）</param>
        /// <param name="request">SQL 執行請求</param>
        /// <returns>執行結果</returns>
        [HmacAuth("sql_execute_secret_key_placeholder")]
        [HttpPost("execute/{connectionStringName?}")]
        [Description("執行 SQL 命令")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult ExecuteSql(string? connectionStringName, [FromBody] SqlRequest request)
        {
            try
            {
                // 驗證請求
                if (request == null || string.IsNullOrWhiteSpace(request.SqlCommand))
                {
                    return BadRequest("SQL 命令不能為空");
                }

                // 基本安全檢查
                if (!SqlUtils.IsSqlCommandSafe(request.SqlCommand))
                {
                    return BadRequest("SQL 命令包含不允許的操作");
                }

                // 取得連接字串
                var connectionString = GetConnectionString(connectionStringName);
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest("找不到指定的資料庫連接字串");
                }

                // 執行 SQL 命令
                var result = SqlUtils.Execute(
                    connectionString,
                    request.SqlCommand,
                    request.IsQuery,
                    request.CommandTimeout
                );

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = request.IsQuery ? "查詢執行成功" : "命令執行成功"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 取得資料庫連接字串
        /// </summary>
        /// <param name="connectionStringName">連接字串名稱（選填）</param>
        /// <returns>連接字串</returns>
        private string? GetConnectionString(string? connectionStringName)
        {
            var appSettings = options.Value;

            // 如果未指定名稱，使用預設連接字串
            if (string.IsNullOrWhiteSpace(connectionStringName))
            {
                connectionStringName = appSettings.DefaultConnectionStringName;
            }

            // 從設定中取得連接字串
            if (appSettings.ConnectionStrings.TryGetValue(connectionStringName, out var connectionString))
            {
                return connectionString;
            }

            return null;
        }
    }
}

