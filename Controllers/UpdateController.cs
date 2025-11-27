using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Web.Administration;
using System.ComponentModel;
using System.Diagnostics;
using website.updater.Filters;
using website.updater.Models;
using website.updater.Utils;

namespace website.updater.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly ZipUtils _zipUtils;

        public UpdateController(IOptions<AppSettings> options, ZipUtils zipUtils)
        {
            _appSettings = options.Value;
            _zipUtils = zipUtils;
        }

        [HmacAuth("402881e69439739b01943992ce840002")]
        [HttpPost("pool/{appPoolName}")]
        [Description("更新程式")]
        [Consumes("multipart/form-data", "application/json")]
        [Produces("application/json")]
        public IActionResult UpdateWebsite(string appPoolName, IFormFile file)
        {
            try
            {
                var destinationPath = CheckAppPoolName(appPoolName);

                if (string.IsNullOrEmpty(destinationPath)) return NotFound($"{appPoolName}尚未定義");

                if (!file.ContentType.StartsWith("application/zip") && !file.ContentType.StartsWith("application/x-zip-compressed")) return BadRequest("檔案格式錯誤，請上傳zip檔案");

                using (ServerManager serverManager = new ServerManager())
                {
                    // 找到指定的應用程式集區
                    ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];

                    if (appPool == null) return NotFound($"找不到名為 {appPoolName} 的應用程式集區。");

                    // 停止應用程式集區
                    appPool.Stop();

                    Thread.Sleep(1000);

                    // 處理更新
                    using (var fileStream = file.OpenReadStream())
                    {
                        _zipUtils.ExtractZipFile(fileStream, destinationPath);
                    }

                    // 啟動應用程式集區
                    appPool.Start();

                    Thread.Sleep(1000);
                    return Ok($"{appPoolName}的程式已成功更新");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HmacAuth("0cefa29e83885cbb01838c324c870000")]
        [HttpPost("pool/{appPoolName}/start")]
        [Description("開啟應用程式集區")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult StartApplicationPool(string appPoolName)
        {
            try
            {
                if (string.IsNullOrEmpty(CheckAppPoolName(appPoolName))) return NotFound($"{appPoolName}尚未定義");

                using (ServerManager serverManager = new ServerManager())
                {
                    // 找到指定的應用程式集區
                    ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];

                    if (appPool == null) return NotFound($"找不到名為 {appPoolName} 的應用程式集區。");

                    appPool.Start();

                    Thread.Sleep(1000);

                    return Ok($"應用程式集區成功啟用（狀態為：{appPool.State.ToString()}）");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HmacAuth("4b1141f07aa426ac017aa443b3450b02")]
        [HttpPost("pool/{appPoolName}/stop")]
        [Description("關閉應用程式集區")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult StopApplicationPool(string appPoolName)
        {
            try
            {
                if (string.IsNullOrEmpty(CheckAppPoolName(appPoolName))) return NotFound($"{appPoolName}尚未定義");

                using (ServerManager serverManager = new ServerManager())
                {
                    // 找到指定的應用程式集區
                    ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];

                    if (appPool == null) return NotFound($"找不到名為 {appPoolName} 的應用程式集區。");

                    appPool.Stop();

                    Thread.Sleep(1000);

                    return Ok($"應用程式集區成功停用（狀態為：{appPool.State.ToString()}）");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HmacAuth("0245a8a4132e41deb924fb750cec6097")]
        [HttpPost("computer/info")]
        [Description("取得電腦資訊")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult GetComputerInfo()
        {
            try
            {
                var memory = GetMemoryInfo();
                return Ok(new
                {
                    CpuUsage = GetCpuUsage(),
                    UsedMemory = memory.usedMb,
                    AvaliableMemory = memory.availableMb,
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        ///  檢查應用程式集區名稱
        /// </summary>
        /// <param name="appPoolName"></param>
        /// <returns></returns>
        private string CheckAppPoolName(string appPoolName)
        {
            return _appSettings.AppSetting.FirstOrDefault(r => r.AppName == appPoolName)?.DirectoryName ?? string.Empty;
        }

        /// <summary>
        /// 取得CPU使用率
        /// </summary>
        /// <returns></returns>
        private float GetCpuUsage()
        {
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ = cpuCounter.NextValue();
            Thread.Sleep(1000); // 必須暫停 1 秒才能取得準確值
            return (float)Math.Round(cpuCounter.NextValue(), 2);
        }

        /// <summary>
        /// 取得記憶體使用率
        /// </summary>
        /// <returns></returns>
        private (float usedMb, float availableMb) GetMemoryInfo()
        {
            using var totalCounter = new PerformanceCounter("Memory", "% Committed Bytes in Use");
            using var availableCounter = new PerformanceCounter("Memory", "Available MBytes");

            float availableMb = availableCounter.NextValue();
            float committedBytes = totalCounter.NextValue();
            float committedMb = committedBytes;

            return (committedMb, availableMb);
        }
    }
}
