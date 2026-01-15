using CenterBackend.common;
using CenterBackend.Constant;
using CenterBackend.Dto;
using CenterBackend.Exceptions;
using CenterBackend.IFileService;
using CenterBackend.IUserServices;
using Masuit.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using System.Net.Mime;
using System.Text.Json;

namespace CenterBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileServices _fileService;
        private readonly IWebHostEnvironment _webHostEnv;
        public FileController(IFileServices fileService, IWebHostEnvironment webHostEnv )
        {
            this._fileService = fileService;
            this._webHostEnv = webHostEnv;
        }

        /// <summary>
        /// 测试
        /// </summary>
        /// <param name="registerDto"></param>
        /// <returns></returns>
        /// <exception cref=""></exception>
        [HttpGet("Test1")]
        public async Task<string> Test1()
        {
            try
            {
                // 操作网站根目录wwwroot下的文件
                var temp = _fileService.GetDateFolderPath(Path.Combine(_webHostEnv.WebRootPath, "Report"), DateTime.Now, true, 2);
                string sourceFilePath = Path.Combine(_webHostEnv.WebRootPath, "Files/model.xlsx");
                string targetFilePath = Path.Combine(temp, $"ReportDaily_{DateTime.Now:yyyyMMddHHmmss}.xlsx");


                bool isSuccess = _fileService.CopyFile(sourceFilePath, targetFilePath, true);

                if (isSuccess)
                {
                    return "文件夹创建+文件复制 操作成功！";
                }
                else
                {
                    return "操作失败，请检查文件路径/权限！";
                }
            }
            catch (Exception ex)
            {
                return $"操作异常：{ex.Message}";
            }
        }

        [HttpPut("test2")]
        public IActionResult DownloadZipFileBig()
        {
            try
            {
                string sourceFolder = Path.Combine(_webHostEnv.WebRootPath, "Files");//方式A：压缩wwwroot内的指定文件夹(推荐，你的项目内文件，如之前的Upload文件夹)
                string zipFileName = $"文件备份_{DateTime.Now:yyyyMMddHHmmss}.zip";// 压缩包名称（带时间戳，避免重复，用户下载的文件名）
                string tempZipPath = Path.Combine(_webHostEnv.WebRootPath, "Temp", zipFileName);// 服务器临时压缩包路径（生成在wwwroot的Temp文件夹，会自动创建）
                bool compressSuccess = _fileService.CompressFolderToZip(sourceFolder, tempZipPath);//调用FileService 压缩文件夹为Zip包
                if (!compressSuccess)
                {
                    return BadRequest("压缩失败，请检查文件路径或权限！");
                }
                var fileBytes = System.IO.File.ReadAllBytes(tempZipPath);// 核心：读取压缩包，返回给浏览器自动下载
                var result = File(fileBytes, MediaTypeNames.Application.Zip, zipFileName);// 关键参数：文件字节流 + MIME类型 + 下载的文件名 → 浏览器自动弹窗下载
                System.IO.File.Delete(tempZipPath);// 4. 重要优化：下载完成后，删除服务器上的临时Zip包，释放磁盘空间

                return result;
            }
            catch (Exception ex)
            {
                return BadRequest($"下载失败：{ex.Message}");
            }
        }

        [HttpGet("test3")]
        public Task<string> Test3()
        {
            var temp = _fileService.GetDateFolderPath(_webHostEnv.WebRootPath, DateTime.Now, true, 1);
            return Task.FromResult(temp);
        }

    }
}
