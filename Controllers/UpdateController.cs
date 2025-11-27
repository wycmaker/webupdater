using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Web.Administration;
using System.ComponentModel;
using website.updater.Filters;
using website.updater.Models;
using website.updater.Utils;

namespace website.updater.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController(IOptions<AppSettings> options) : ControllerBase
    {

        [HmacAuth("402881e69439739b01943992ce840002")]
        [HttpPost("pool/{appPoolName}")]
        [Description("更新程式")]
        [Consumes("multipart/form-data", "application/json")]
        [Produces("application/json")]
        public IActionResult UpdateWebsite(string appPoolName, IFormFile file)
        {
            try
            {
                var (destinationPath, backupPath) = AppPoolUtils.CheckAppPoolName(options.Value, appPoolName);

                if (string.IsNullOrEmpty(destinationPath)) return NotFound($"{appPoolName}尚未定義");

                if (!file.ContentType.StartsWith("application/zip") && !file.ContentType.StartsWith("application/x-zip-compressed")) return BadRequest("檔案格式錯誤，請上傳zip檔案");

                using ServerManager serverManager = new();
                // 找到指定的應用程式集區
                ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];

                if (appPool == null) return NotFound($"找不到名為 {appPoolName} 的應用程式集區。");

                // 停止應用程式集區
                appPool.Stop();

                Thread.Sleep(1000);

                // 執行備份
                if (!string.IsNullOrEmpty(backupPath))
                {
                    AppPoolUtils.BackupDirectory(destinationPath, backupPath, appPoolName);
                }

                // 處理更新
                using (var fileStream = file.OpenReadStream())
                {
                    ZipUtils.ExtractZipFile(fileStream, destinationPath);
                }

                // 啟動應用程式集區
                appPool.Start();

                Thread.Sleep(1000);
                return Ok($"{appPoolName}的程式已成功更新");
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
                var (destination, backup) = AppPoolUtils.CheckAppPoolName(options.Value, appPoolName);
                if (string.IsNullOrEmpty(destination)) return NotFound($"{appPoolName}尚未定義");

                using ServerManager serverManager = new();
                // 找到指定的應用程式集區
                ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];

                if (appPool == null) return NotFound($"找不到名為 {appPoolName} 的應用程式集區。");

                appPool.Start();

                Thread.Sleep(1000);

                return Ok($"應用程式集區成功啟用（狀態為：{appPool.State}）");
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
                var (destination, backup) = AppPoolUtils.CheckAppPoolName(options.Value, appPoolName);
                if (string.IsNullOrEmpty(destination)) return NotFound($"{appPoolName}尚未定義");

                using ServerManager serverManager = new();
                // 找到指定的應用程式集區
                ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];

                if (appPool == null) return NotFound($"找不到名為 {appPoolName} 的應用程式集區。");

                appPool.Stop();

                Thread.Sleep(1000);

                return Ok($"應用程式集區成功停用（狀態為：{appPool.State}）");
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
                var (usedMb, availableMb) = HardwareUtils.GetMemoryInfo();
                return Ok(new
                {
                    CpuUsage = HardwareUtils.GetCpuUsage(),
                    UsedMemory = usedMb,
                    AvaliableMemory = availableMb,
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
