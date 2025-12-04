using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace website.updater.Filters
{
    public class HmacAuthAttribute : TypeFilterAttribute
    {
        public HmacAuthAttribute(string secret) : base(typeof(HMACFilter))
        {
            // 傳遞 API Key 到過濾器
            Arguments = [secret];
        }
    }

    public class HMACFilter(string secret) : IAsyncActionFilter
    {

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 確保傳入的 API Key 和簽名都存在
            var signature = context.HttpContext.Request.Headers["X-Signature"].FirstOrDefault();
            var apiKey = context.HttpContext.Request.Headers["X-API-KEY"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(apiKey))
            {
                context.Result = new BadRequestObjectResult("Missing Signature or API Key");
                return;
            }

            // 計算簽名
            var validSignature = GenerateHmacSignature(apiKey);

            // 驗證簽名
            if (signature != validSignature)
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            await next();
        }

        /// <summary>
        /// 產生Signature
        /// </summary>
        /// <param name="apiKey">API密鑰</param>
        /// <returns></returns>
        private string GenerateHmacSignature(string apiKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var message = $"{apiKey}_{DateTime.Now:yyyyMMdd}_Mobagel";
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
