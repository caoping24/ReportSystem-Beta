using CenterBackend.common;
using CenterBackend.Dto;
using CenterBackend.Exceptions;
using CenterBackend.IFileService;
using CenterBackend.IReportServices;
using CenterBackend.Models;
using CenterBackend.Services;
using Masuit.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using NPOI.HPSF;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Net;
using System.Net.Mime;

namespace CenterBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService reportService;
        private readonly IFileServices _fileService;
        private readonly IWebHostEnvironment _webHostEnv;

        public ReportController(IReportService reportService, IFileServices fileService, IWebHostEnvironment webHostEnv)
        {
            this.reportService = reportService;
            this._fileService = fileService;
            this._webHostEnv = webHostEnv;
        }
        /// <summary>
        /// 根据dto.Type 统计数据并且插入表中
        /// </summary>
        /// <param name="_AddReportDailyDto"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        [HttpPost("AnalysesInsert")] 
        public async Task<BaseResponse<bool>> AnalysesInsert([FromBody] CalculateAndInsertDto _CalculateAndInsertDto)
        {
            if (_CalculateAndInsertDto.Type == 0 )
            {
                throw new BusinessException(ErrorCode.PARAMS_ERROR, "类型错误");
            }
            var result = await reportService.DataAnalyses(_CalculateAndInsertDto);
            return ResultUtils<bool>.Success(result);
        }
        /// <summary>
        ///  根据传入时间查询数据库,生成报表 Type 表示不同的报表类型
        /// </summary>
        /// <param name="CreateReportDto"></param>
        /// <returns></returns>
        [HttpPost("BuildReport")]
        public async Task<IActionResult> CreateAndBuildReport([FromBody] CreateReportDto _CreateReportDto)
        {
            var reportFileRoot = Path.Combine(_webHostEnv.WebRootPath, "Report");//所有报表汇总文件夹
            DateTime tempTime = _CreateReportDto.Time;
            int reportType = _CreateReportDto.Type;

            var filePathAndName = _fileService.GetDateFolderPathAndName(reportFileRoot, tempTime);
            if (string.IsNullOrWhiteSpace(filePathAndName.DailyFileName)) return BadRequest("获取文件路径失败，请检查传入日期格式！");
            string? XlsxFilesPath;
            string? XlsxFilesFullPath;
            string? modelFilePath;
            try
            {
                switch (_CreateReportDto.Type)
                {
                    case 1: //Daily
                        XlsxFilesPath = filePathAndName.DailyFilesPath;
                        XlsxFilesFullPath = filePathAndName.DailyFilesFullPath;
                        modelFilePath = Path.Combine(_webHostEnv.WebRootPath, "Model\\Model-20260116.xlsx");//日报表模板路径
                        break;
                    case 2: //Weekly
                        XlsxFilesPath = filePathAndName.WeeklyFilesPath;
                        XlsxFilesFullPath = filePathAndName.WeeklyFilesFullPath;
                        modelFilePath = Path.Combine(_webHostEnv.WebRootPath, "Model\\Model-20260116.xlsx");//日报表模板路径
                        break;
                    case 3: //Monthly
                        XlsxFilesPath = filePathAndName.MonthlyFilesPath;
                        XlsxFilesFullPath = filePathAndName.MonthlyFilesFullPath;
                        modelFilePath = Path.Combine(_webHostEnv.WebRootPath, "Model\\Model-20260116.xlsx");//日报表模板路径
                        break;
                    case 4: //Yearly
                        XlsxFilesPath = filePathAndName.YearlyFilesPath;
                        XlsxFilesFullPath = filePathAndName.YearlyFilesFullPath;
                        modelFilePath = Path.Combine(_webHostEnv.WebRootPath, "Model\\Model-20260116.xlsx");//日报表模板路径
                        break;
                    default:
                        return new BadRequestObjectResult(new { success = false, msg = "ReportType不存在" });
                }
                _fileService.CreateFolder(XlsxFilesPath);//自动创建文件夹
                return await reportService.WriteXlsxAndSave(modelFilePath, XlsxFilesFullPath, tempTime, reportType);
            }
            catch (Exception ex)
            {
                return BadRequest($"操作异常：{ex.Message}");
            }
        }
        /// <summary>
        /// 下载单个excle文件
        /// </summary>
        /// <param name="loginDto"></param>
        /// <returns></returns>
        [HttpPost("DownloadExcel")]
        public IActionResult DownloadFile([FromBody] FileDownloadExcleDto _fileDownloadExcleDto)
        {
            var modelFilePath = Path.Combine(_webHostEnv.WebRootPath, "Report");//日报表模板路径
            var PathAndFileName = _fileService.GetDateFolderPathAndName(modelFilePath, _fileDownloadExcleDto.Time);
            var DownloadfilePath = string.Empty;
            var DownloadfileName = string.Empty;
            switch (_fileDownloadExcleDto.Type)
                {
                case 1: //Daily
                    DownloadfilePath = PathAndFileName.DailyFilesPath;
                    DownloadfileName = PathAndFileName.DailyFileName;
                    break;
                case 2: //Weekly
                    DownloadfilePath = PathAndFileName.WeeklyFilesPath;
                    DownloadfileName = PathAndFileName.WeeklyFileName;
                    break;
                case 3: //Monthly
                    DownloadfilePath = PathAndFileName.MonthlyFilesPath;
                    DownloadfileName = PathAndFileName.MonthlyFileName;
                    break;
                case 4: //Yearly
                    DownloadfilePath = PathAndFileName.YearlyFilesPath;
                    DownloadfileName = PathAndFileName.YearlyFileName;
                    break;
                default:
                    return BadRequest("类型错误，请检查传入类型！");
            }
            var (fileStream, encodeFileName) = _fileService.DownloadSingleFile(DownloadfilePath, DownloadfileName);
            if (fileStream == null)
            {
                return NotFound("文件不存在。");
            }
            string fileName = PathAndFileName.DailyFileName;
            Response.Headers.Append("Content-Disposition", $"attachment;filename={Uri.EscapeDataString(fileName)}");
            return File(fileStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

        ///// 原来测试用////////////////////////////////
        //// 1. 先创建临时流写入Excel，再转字节数组，避免流被隐式关闭
        //      byte[] excelBytes;
        //    using (var tempStream = new MemoryStream())
        //    {
        //        // 创建Excel工作簿（业务逻辑完全不变）
        //        IWorkbook workbook = new XSSFWorkbook();
        //        ISheet worksheet = workbook.CreateSheet($"{id}号报表数据");
        //        // 写入表头（逻辑不变）
        //        IRow headerRow = worksheet.CreateRow(0);
        //        headerRow.CreateCell(0).SetCellValue("报表ID");
        //        headerRow.CreateCell(1).SetCellValue("报表类型");
        //        headerRow.CreateCell(2).SetCellValue("报表内容");
        //        headerRow.CreateCell(3).SetCellValue("生成时间");
        //        // 写入业务数据（逻辑完全不变）
        //        IRow dataRow = worksheet.CreateRow(1);
        //        dataRow.CreateCell(0).SetCellValue(id);
        //        dataRow.CreateCell(1).SetCellValue(tabKey == 1 ? "日报表" : tabKey == 2 ? "周报表" : tabKey == 3 ? "月报表" : "年报表");
        //        dataRow.CreateCell(2).SetCellValue("报表数据内容");
        //        dataRow.CreateCell(3).SetCellValue(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        //        // 自动调整列宽（逻辑不变）
        //        for (int i = 0; i < 4; i++)
        //        {
        //            worksheet.AutoSizeColumn(i);
        //        }
        //        // 将工作簿写入临时流
        //        workbook.Write(tempStream);
        //        // 转字节数组（核心：脱离原流的依赖）
        //        excelBytes = tempStream.ToArray();
        //    }
        //    // 2. 基于字节数组创建新的MemoryStream，完全避免流关闭问题
        //    var resultStream = new MemoryStream(excelBytes);
        //    resultStream.Position = 0; // 此时流绝对不会被关闭
        //    // 以下代码完全不变
        //    string fileName = $"报表_{id}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        //    Response.Headers.Append("Content-Disposition", $"attachment;filename={Uri.EscapeDataString(fileName)}");
        //    // 返回File结果，框架会自动处理流的释放
        //    return File(resultStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("Test")]// Get测试从服务器下载文件
        public IActionResult Test()
        {
            try
            {
                string downloadFilePath = _webHostEnv.WebRootPath;
                string downloadFileName = "comm.txt";
                var (fileStream, encodeFileName) = _fileService.DownloadSingleFile(downloadFilePath, downloadFileName);

                if (fileStream == null) return NotFound("文件不存在。");

                if (fileStream.CanSeek) fileStream.Position = 0;

                return File(fileStream, "application/octet-stream; charset=utf-8", downloadFileName);

            }
            catch (IOException ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"文件下载失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"系统异常：{ex.Message}");
            }
        }
    }
}
