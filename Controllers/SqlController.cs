using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
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
        public async Task<IActionResult> ExecuteSql(string? connectionStringName, [FromBody] SqlRequest request)
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
                    await ConvertToSqlCommand(request.SqlCommand, request.Iv),
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

        /// <summary>
        /// 轉換SQL語法
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private async Task<string> ConvertToSqlCommand(string command, string ivBase64)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes("3062147169af4a619a25008379379e4f");

                var cipherBytes = Convert.FromBase64String(command);
                var iv = Convert.FromBase64String(ivBase64);

                var plaintext = new byte[cipherBytes.Length - 16]; // AES-GCM tag = 16 bytes
                var tag = new byte[16];

                Buffer.BlockCopy(cipherBytes, cipherBytes.Length - 16, tag, 0, 16);
                var cipher = new byte[cipherBytes.Length - 16];
                Buffer.BlockCopy(cipherBytes, 0, cipher, 0, cipher.Length);

                using var aes = new AesGcm(key, 16);

                aes.Decrypt(iv, cipher, tag, plaintext);

                return Encoding.UTF8.GetString(plaintext);
            }
            catch
            {
                // 如果不是有效的 Base64 字串，直接返回原始命令
                return command;
            }
        }
    }
}

