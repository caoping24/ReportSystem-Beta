using CenterBackend.common;
using CenterBackend.Constant;
using CenterBackend.Dto;
using CenterBackend.Exceptions;
using CenterBackend.IFileService;
using CenterBackend.IReportServices;
using CenterBackend.Models;
using Masuit.Tools;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Text.Json;

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
        /// 根据dto.Type 进行日报统计表插入(Type实际暂时没有使用，只是判断是否为空)
        /// </summary>
        /// <param name="_AddReportDailyDto"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        [HttpPost("DailyInsert")]
        public async Task<BaseResponse<bool>> DailyInsert([FromBody] DailyInsertDto _DailyInsertDto)
        {
            if (_DailyInsertDto.ReportType.IsNullOrEmpty())
            {
                throw new BusinessException(ErrorCode.PARAMS_ERROR, "添加目标不能为空");
            }
            var result = await reportService.DailyCalculateAndInsertAsync(_DailyInsertDto);
            return ResultUtils<bool>.Success(result);
        }
        /// <summary>
        /// get测试
        /// </summary>
        /// <param name="loginDto"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<string> TEST()
        {
            return "Hello World!";
        }

        /// <summary>
        /// CreateAndBuildDailyReport 根据传入时间查询数据库,生成报表(日)
        /// </summary>
        /// <param name="loginDto"></param>
        /// <returns></returns>
        [HttpPost("CreateAndBuildDailyReport")]
        public async Task<IActionResult> CreateAndBuildDailyReport([FromBody] CreateReportDto _CreateReportDto)
        {

            var modelFilePath = Path.Combine(_webHostEnv.WebRootPath, "Model\\Model-20260116.xlsx");//日报表模板路径
            var reportFileRoot = Path.Combine(_webHostEnv.WebRootPath, "Report");//所有报表汇总文件夹

            var tempTime = _CreateReportDto.AddDate;
            var filePathAndName = new FilePathAndName();

            filePathAndName = _fileService.GetDateFolderPathAndName(reportFileRoot, tempTime);

            if (string.IsNullOrWhiteSpace(filePathAndName.DailyFileName) )
            {
                return BadRequest("获取文件路径失败，请检查传入日期是否合法！");
            }
            try
            {
                _fileService.CreateFolder(filePathAndName.DailyFilesPath);//自动创建文件夹
                return await reportService.WriteXlsxAndSave(modelFilePath, filePathAndName.DailyFilesFullPath, tempTime);
            }
            catch (Exception ex)
            {
                return BadRequest($"操作异常：{ex.Message}");
            }
            //return await reportService.ExportReport();
        }

        /// <summary>
        /// ExportExcel 测试使用
        /// </summary>
        /// <param name="loginDto"></param>
        /// <returns></returns>
        [HttpGet("ExportExcel")]
        public IActionResult ExportExcel([FromQuery] int id, [FromQuery] int tabKey)
        {
            // 1. 先创建临时流写入Excel，再转字节数组，避免流被隐式关闭
            byte[] excelBytes;
            using (var tempStream = new MemoryStream())
            {
                // 创建Excel工作簿（业务逻辑完全不变）
                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet($"{id}号报表数据");

                // 写入表头（逻辑不变）
                IRow headerRow = worksheet.CreateRow(0);
                headerRow.CreateCell(0).SetCellValue("报表ID");
                headerRow.CreateCell(1).SetCellValue("报表类型");
                headerRow.CreateCell(2).SetCellValue("报表内容");
                headerRow.CreateCell(3).SetCellValue("生成时间");

                // 写入业务数据（逻辑完全不变）
                IRow dataRow = worksheet.CreateRow(1);
                dataRow.CreateCell(0).SetCellValue(id);
                dataRow.CreateCell(1).SetCellValue(tabKey == 1 ? "日报表" : tabKey == 2 ? "周报表" : tabKey == 3 ? "月报表" : "年报表");
                dataRow.CreateCell(2).SetCellValue("报表数据内容");
                dataRow.CreateCell(3).SetCellValue(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                // 自动调整列宽（逻辑不变）
                for (int i = 0; i < 4; i++)
                {
                    worksheet.AutoSizeColumn(i);
                }

                // 将工作簿写入临时流
                workbook.Write(tempStream);
                // 转字节数组（核心：脱离原流的依赖）
                excelBytes = tempStream.ToArray();
            }

            // 2. 基于字节数组创建新的MemoryStream，完全避免流关闭问题
            var resultStream = new MemoryStream(excelBytes);
            resultStream.Position = 0; // 此时流绝对不会被关闭

            // 以下代码完全不变
            string fileName = $"报表_{id}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            Response.Headers.Append("Content-Disposition", $"attachment;filename={Uri.EscapeDataString(fileName)}");

            // 返回File结果，框架会自动处理流的释放
            return File(resultStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}
