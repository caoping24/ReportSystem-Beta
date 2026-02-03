using CenterBackend.IFileService;
using CenterBackend.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace CenterBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelpController : ControllerBase
    {
        private readonly IFileServices _fileService;
        private readonly IWebHostEnvironment _webHostEnv;
        private readonly IAppLogger _logger;
        public HelpController(IFileServices fileService, IWebHostEnvironment webHostEnv, IAppLogger _IAppLogger)
        {
            this._fileService = fileService;
            this._webHostEnv = webHostEnv;
            this._logger = _IAppLogger;
        }


        [HttpGet("Version")] // 显式指定GET，路由：api/Help/Version（全局唯一）
        public async Task<IActionResult> Version()
        {
            // 获取当前项目程序集（核心代码：获取编译时间）
            var assembly = Assembly.GetExecutingAssembly();
            var compileFileInfo = new FileInfo(assembly.Location);
            // 编译时间（UTC版，工业场景推荐，避免时区问题）
            var compileTimeUtc = compileFileInfo.LastWriteTimeUtc;
            // 可选：转本地时间（根据你的系统需求选择）
            var compileTimeLocal = compileTimeUtc.ToLocalTime();
            // 组装返回结果，包含版本号+编译时间
            return Ok(new
            {
                success = true,
                msg = "Version: 1.0.0.1",
                compileTimeUtc = compileTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"), // UTC编译时间
                compileTimeLocal = compileTimeLocal.ToString("yyyy-MM-dd HH:mm:ss") // 本地编译时间
            });
        }
    }
}