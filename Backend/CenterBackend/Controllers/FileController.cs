using CenterBackend.Dto;
using CenterBackend.IFileService;
using CenterBackend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Web;

namespace CenterBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileServices _fileService;
        private readonly IWebHostEnvironment _webHostEnv;
        public FileController(IFileServices fileService, IWebHostEnvironment webHostEnv)
        {
            this._fileService = fileService;
            this._webHostEnv = webHostEnv;
        }

        /// <summary>
        /// 在Report目录下创建按日期命名所有的文件夹
        /// </summary>
        /// <returns></returns>
        /// <exception cref=""></exception>
        [HttpGet("CreateDateFolderTest")]
        public async Task<IActionResult> Test1()
        {
            try
            {
                //var pathAndName = _fileService.GetDateFolderPathAndName(Path.Combine(_webHostEnv.WebRootPath, "Report"), DateTime.Now);
                //string sourceFilePath = Path.Combine(_webHostEnv.WebRootPath, "Files/Model-20260116.xlsx");
                _fileService.CreateDateFolder(Path.Combine(_webHostEnv.WebRootPath, "Report"), DateTime.Now);
                return new OkObjectResult(new { success = true, msg = "操作成功" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, msg = $"创建文件夹失败：{ex.Message}" });
            }
        }
        /// <summary>
        /// 根绝日期获取对应的文件夹路径和文件名测试
        /// </summary>
        /// <returns></returns>
        /// <exception cref=""></exception>
        [HttpGet("GetFolderPathTest")]
        public Task<FilePathAndName> Test3()
        {
            var temp = _fileService.GetDateFolderPathAndName(Path.Combine(_webHostEnv.WebRootPath, "Report"), DateTime.Now);
            return Task.FromResult(temp);
        }

        /// <summary>
        /// 下载大文件压缩包测试
        /// </summary>
        /// <returns></returns>
        /// <exception cref=""></exception>
        [HttpPut("ZipFileBigTest")]
        public IActionResult DownloadZipFileBig([FromBody] FileDownloadZIPDto _FileDownloadZIPDto)
        {
            try
            {
                string sourceFolder = Path.Combine(_webHostEnv.WebRootPath, "Report");//方式A：压缩wwwroot内的指定文件夹(推荐，你的项目内文件，如之前的Upload文件夹)
                string zipFileName = $"文件备份_{DateTime.Now:yyyyMMddHHmmss}.zip";// 压缩包名称（带时间戳，避免重复，用户下载的文件名）
                string tempZipPath = Path.Combine(_webHostEnv.WebRootPath, "Temp", zipFileName);// 服务器临时压缩包路径（生成在wwwroot的Temp文件夹，会自动创建）
                bool compressSuccess = _fileService.CompressFolderToZip(sourceFolder, tempZipPath);//调用FileService 压缩文件夹为Zip包
                if (!compressSuccess)
                {
                    return BadRequest("压缩失败，请检查文件路径或权限！");
                }
                var fileBytes = System.IO.File.ReadAllBytes(tempZipPath);//读取压缩包，返回给浏览器自动下载
                string encodeFileName = HttpUtility.UrlEncode(zipFileName, System.Text.Encoding.UTF8);// 解决中文文件名下载乱码问题
                var result = File(fileBytes, MediaTypeNames.Application.Zip, zipFileName);// 文件字节流 + MIME类型 + 下载的文件名 → 浏览器自动弹窗下载
                System.IO.File.Delete(tempZipPath);
                return result;
            }
            catch (Exception ex)
            {
                return BadRequest($"下载失败：{ex.Message}");
            }
        }
    }
}
