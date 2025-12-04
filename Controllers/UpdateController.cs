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

        [HmacAuth("nextjs_update_secret_key_placeholder")]
        [HttpPost("nextjs/{projectName}")]
        [Description("更新 NextJS 專案")]
        [Consumes("multipart/form-data", "application/json")]
        [Produces("application/json")]
        public IActionResult UpdateNextJSProject(string projectName, IFormFile file)
        {
            try
            {
                // 檢查專案設定
                var project = options.Value.NextJSProjects.FirstOrDefault(p => p.ProjectName == projectName);
                if (project == null)
                {
                    return NotFound($"{projectName} 專案尚未定義");
                }

                // 驗證檔案格式
                if (!file.ContentType.StartsWith("application/zip") &&
                    !file.ContentType.StartsWith("application/x-zip-compressed"))
                {
                    return BadRequest("檔案格式錯誤，請上傳zip檔案");
                }

                // 步驟 1: 停止 PM2 程序
                var (stopSuccess, stopMessage) = PM2Utils.StopProcess(project.Pm2Id);
                if (!stopSuccess)
                {
                    return BadRequest($"停止 PM2 程序失敗: {stopMessage}");
                }

                Thread.Sleep(1000);

                // 步驟 2: 備份舊檔案（排除 node_modules）
                if (!string.IsNullOrEmpty(project.BackupPath))
                {
                    NextJSUtils.BackupNextJSProject(project.DirectoryPath, project.BackupPath, projectName);
                }

                // 步驟 3: 清除舊檔案
                NextJSUtils.ClearProjectDirectory(project.DirectoryPath);

                // 步驟 4: 解壓縮新檔案
                using (var fileStream = file.OpenReadStream())
                {
                    ZipUtils.ExtractZipFile(fileStream, project.DirectoryPath);
                }

                // 步驟 5: 啟動 PM2 程序
                var (startSuccess, startMessage) = PM2Utils.StartProcess(project.Pm2Id);
                if (!startSuccess)
                {
                    return BadRequest($"啟動 PM2 程序失敗: {startMessage}");
                }

                Thread.Sleep(1000);

                // 步驟 6: 儲存 PM2 設定
                var (saveSuccess, saveMessage) = PM2Utils.Save();
                if (!saveSuccess)
                {
                    Console.WriteLine($"警告: {saveMessage}");
                    // 不中斷流程，僅記錄警告
                }

                // 步驟 7: 完成
                return Ok(new
                {
                    message = $"{projectName} 專案已成功更新",
                    details = new
                    {
                        pm2Id = project.Pm2Id,
                        directoryPath = project.DirectoryPath,
                        backupPath = project.BackupPath
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
