using CenterBackend.common;
using CenterBackend.Constant;
using CenterBackend.Dto;
using CenterBackend.Exceptions;
using CenterBackend.IFileService;
using CenterBackend.IReportServices;
using CenterBackend.Models;
using CenterReport.Repository;
using CenterReport.Repository.Models;
using Mapster;
using Masuit.Tools;
using Masuit.Tools.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySqlConnector;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System.Text.Json;

namespace CenterBackend.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository<SourceData1> _sourceData1;
        private readonly IReportRepository<SourceData2> _sourceData2;
        private readonly IReportRepository<SourceData3> _sourceData3;
        //private readonly IReportRepository<SourceData4> _sourceData4;
        //private readonly IReportRepository<SourceData5> _sourceData5;
        private readonly IReportRepository<HourlyDataStatistic> _hourlyDataStatistics;
        private readonly IReportUnitOfWork _reportUnitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // 构造函数注入：按顺序注入5个SourceData仓储 + 原有依赖，一一对应赋值
        public ReportService(IReportRepository<SourceData1> sourceData1,
                             IReportRepository<SourceData2> sourceData2,
                             IReportRepository<SourceData3> sourceData3,
                             //IReportRepository<SourceData4> sourceData4,
                             //IReportRepository<SourceData5> sourceData5,
                             IReportRepository<HourlyDataStatistic> hourlyDataStatistics,
                             IReportUnitOfWork reportUnitOfWork,
                             IHttpContextAccessor httpContextAccessor)
        {
            this._sourceData1 = sourceData1;
            this._sourceData2 = sourceData2;
            this._sourceData3 = sourceData3;
            //this._sourceData4 = sourceData4;
            //this._sourceData5 = sourceData5;
            this._hourlyDataStatistics = hourlyDataStatistics;
            this._reportUnitOfWork = reportUnitOfWork;
            this._httpContextAccessor = httpContextAccessor;
        }

        //根据id删除对应数据
        public async Task<bool> DeleteReport( long id, DailyInsertDto _AddReportDailyDto)
        {
            //switch (AddReportDto.Target)
            //{
            //    case "SourceData":
            //        var source = await SourceDatas.GetByIdAsync(id);
            //        if (source == null) return false;
            //        await SourceDatas.DeleteByIdAsync(id);
            //        break;
            //    case "HourlyDataStatistic":
            //        var hour = await HourlyDataStatistics.GetByIdAsync(id);
            //        if (hour == null) return false;
            //        await HourlyDataStatistics.DeleteByIdAsync(id);
            //        break;
            //    default:
            //        throw new ArgumentException("未知的删除目标", AddReportDto.Target);
            //}
            //    await reportUnitOfWork.SaveChangesAsync();
            return true;
        }
        //统计每日数据写入对应表
        public async Task<bool> AddReport(DailyInsertDto _AddReportDailyDto)
        {
            //switch (AddReportDto.Target)
            //{
            //    case "HourlyDataStatistic":
            //        var source = await SourceDatas.GetByDataTimeAsync(AddReportDto.Datetime);
            //        if (source == null) return false; // 未找到数据，按需可抛异常或返回 false
            //        var stat = source.Adapt<HourlyDataStatistic>();// 映射单条 SourceData 到 HourlyDataStatistic 并入库
            //        stat.Id = 0; // 确保 EF 分配新 Id

            //        await HourlyDataStatistics.AddAsync(stat);
            //        break;
            //    default:
            //        throw new ArgumentException("未知的添加目标", AddReportDto.Target);
            //}
            //await reportUnitOfWork.SaveChangesAsync();
            return true;
        }


        /// <summary>
        /// 统计每日数据写入对应表
        /// </summary>
        public async Task<bool> DailyCalculateAndInsertAsync(DailyInsertDto _AddReportDailyDto)
        {

            DateTime StartTime = _AddReportDailyDto.AddDate.Date;
            DateTime StopTime = _AddReportDailyDto.AddDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            HourlyDataStatistic targetModel = new HourlyDataStatistic()
            {
                createdtime = DateTime.Now,
                PH = 80,
            };

            await CalculateSourceData1Async(StartTime, StopTime, targetModel);
            //await CalculateSourceData2Async(startTime, endTime, targetModel);
            //await CalculateSourceData3Async(startTime, endTime, targetModel);
            //await CalculateSourceData4Async(startTime, endTime, targetModel);
            //await CalculateSourceData5Async(startTime, endTime, targetModel);

            await _hourlyDataStatistics.AddAsync(targetModel);
            await _reportUnitOfWork.SaveChangesAsync();
            return true;
        }



        private async Task CalculateSourceData1Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            // cell1-cell10 完整赋值 - 按平均值、差值、总和循环重复，带注释+空值处理
            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和

            target.cell4 = dataList.Select(x => x.cell4 ?? 0).Average();//平均值
            target.cell5 = (dataList.Last().cell5 ?? 0) - (dataList.First().cell5 ?? 0);//差值
            target.cell6 = dataList.Select(x => x.cell6 ?? 0).Sum();//总和

            target.cell7 = dataList.Select(x => x.cell7 ?? 0).Average();//平均值
            target.cell8 = (dataList.Last().cell8 ?? 0) - (dataList.First().cell8 ?? 0);//差值
            target.cell9 = dataList.Select(x => x.cell9 ?? 0).Sum();//总和

            target.cell10 = dataList.Select(x => x.cell10 ?? 0).Average();//平均值
        }



        private async Task CalculateSourceData2Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和
        }



        private async Task CalculateSourceData3Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和
        }



        private async Task CalculateSourceData4Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和
        }



        private async Task CalculateSourceData5Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和
        }



        /// <summary>
        /// 从模板文件创建文件流，然后按区域写数据（优化完整版）
        /// </summary>
        /// <param name="TemplateFullPath">Excel模板完整路径</param>
        /// <param name="TargetSavePath">生成文件的保存路径</param>
        /// <param name="ReportTime">报表日期</param>
        /// <returns></returns>
        public async Task<IActionResult> WriteXlsxAndSave(string ModelFullPath, string TargetPullPath, DateTime ReportTime)
        {
            try
            {
                var dataList = await _sourceData1.GetByDayAsync(ReportTime);
                if (dataList == null || !dataList.Any())
                {
                    var msg = $"指定日期【{ReportTime:yyyy-MM-dd}】无数据，跳过Excel处理";
                    return new OkObjectResult(new { success = true, msg });
                }
                using var templateStream = new FileStream(ModelFullPath, FileMode.Open, FileAccess.Read);
                using var workbook = new XSSFWorkbook(templateStream);
                ISheet sheet = workbook.GetSheetAt(0);
                sheet.ForceFormulaRecalculation = false;//批量写入关闭公式自动计算，大幅提升写入速度

                WriteXlsxRange1(workbook, sheet, 5, dataList);

                // 4. 保存文件到指定路径，强制刷新缓存，杜绝文件损坏
                using var outputStream = new FileStream(TargetPullPath, FileMode.Create, FileAccess.Write);
                workbook.Write(outputStream);
                return new OkObjectResult(new { success = true, msg = "Excel生成成功" });
            }
            catch (Exception ex)
            {
                var errorMsg = $"生成Excel报表异常，报表日期：{ReportTime:yyyy-MM-dd}，异常信息：{ex.ToString()}";
                return new BadRequestObjectResult(new { success = false, msg = $"操作异常：{ex.Message}" });
            }
        }

        /// <summary>
        /// 按区域写Xlsx数据 1
        /// </summary>
        private static bool WriteXlsxRange1(XSSFWorkbook srcWorkbook, ISheet srcSheet , int startRow, IEnumerable<SourceData1> dataList)
        {

            for (int i = 0; i < dataList.Count(); i++)
            {
                var data = dataList.ElementAt(i);
                int rowIndex = startRow + i;

                // 从Excel第2列开始写入，对应cell1-cell42 共42条数据 | 已注释 29-35条(cell29-cell35)
                if (data.cell1 != null) { SetXlsxCellValue(srcSheet, rowIndex, 2, (float)Math.Round(Convert.ToSingle(data.cell1), 2)); }
                if (data.cell2 != null) { SetXlsxCellValue(srcSheet, rowIndex, 3, (float)Math.Round(Convert.ToSingle(data.cell2), 2)); }
                if (data.cell3 != null) { SetXlsxCellValue(srcSheet, rowIndex, 4, (float)Math.Round(Convert.ToSingle(data.cell3), 2)); }
                if (data.cell4 != null) { SetXlsxCellValue(srcSheet, rowIndex, 5, (float)Math.Round(Convert.ToSingle(data.cell4), 2)); }
                if (data.cell5 != null) { SetXlsxCellValue(srcSheet, rowIndex, 6, (float)Math.Round(Convert.ToSingle(data.cell5), 2)); }
                if (data.cell6 != null) { SetXlsxCellValue(srcSheet, rowIndex, 7, (float)Math.Round(Convert.ToSingle(data.cell6), 2)); }
                if (data.cell7 != null) { SetXlsxCellValue(srcSheet, rowIndex, 8, (float)Math.Round(Convert.ToSingle(data.cell7), 2)); }
                if (data.cell8 != null) { SetXlsxCellValue(srcSheet, rowIndex, 9, (float)Math.Round(Convert.ToSingle(data.cell8), 2)); }
                if (data.cell9 != null) { SetXlsxCellValue(srcSheet, rowIndex, 10, (float)Math.Round(Convert.ToSingle(data.cell9), 2)); }
                if (data.cell10 != null) { SetXlsxCellValue(srcSheet, rowIndex, 11, (float)Math.Round(Convert.ToSingle(data.cell10), 2)); }
                if (data.cell11 != null) { SetXlsxCellValue(srcSheet, rowIndex, 12, (float)Math.Round(Convert.ToSingle(data.cell11), 2)); }
                if (data.cell12 != null) { SetXlsxCellValue(srcSheet, rowIndex, 13, (float)Math.Round(Convert.ToSingle(data.cell12), 2)); }
                if (data.cell13 != null) { SetXlsxCellValue(srcSheet, rowIndex, 14, (float)Math.Round(Convert.ToSingle(data.cell13), 2)); }
                if (data.cell14 != null) { SetXlsxCellValue(srcSheet, rowIndex, 15, (float)Math.Round(Convert.ToSingle(data.cell14), 2)); }
                if (data.cell15 != null) { SetXlsxCellValue(srcSheet, rowIndex, 16, (float)Math.Round(Convert.ToSingle(data.cell15), 2)); }
                if (data.cell16 != null) { SetXlsxCellValue(srcSheet, rowIndex, 17, (float)Math.Round(Convert.ToSingle(data.cell16), 2)); }
                if (data.cell17 != null) { SetXlsxCellValue(srcSheet, rowIndex, 18, (float)Math.Round(Convert.ToSingle(data.cell17), 2)); }
                if (data.cell18 != null) { SetXlsxCellValue(srcSheet, rowIndex, 19, (float)Math.Round(Convert.ToSingle(data.cell18), 2)); }
                if (data.cell19 != null) { SetXlsxCellValue(srcSheet, rowIndex, 20, (float)Math.Round(Convert.ToSingle(data.cell19), 2)); }
                if (data.cell20 != null) { SetXlsxCellValue(srcSheet, rowIndex, 21, (float)Math.Round(Convert.ToSingle(data.cell20), 2)); }
                if (data.cell21 != null) { SetXlsxCellValue(srcSheet, rowIndex, 22, (float)Math.Round(Convert.ToSingle(data.cell21), 2)); }
                if (data.cell22 != null) { SetXlsxCellValue(srcSheet, rowIndex, 23, (float)Math.Round(Convert.ToSingle(data.cell22), 2)); }
                if (data.cell23 != null) { SetXlsxCellValue(srcSheet, rowIndex, 24, (float)Math.Round(Convert.ToSingle(data.cell23), 2)); }
                if (data.cell24 != null) { SetXlsxCellValue(srcSheet, rowIndex, 25, (float)Math.Round(Convert.ToSingle(data.cell24), 2)); }
                if (data.cell25 != null) { SetXlsxCellValue(srcSheet, rowIndex, 26, (float)Math.Round(Convert.ToSingle(data.cell25), 2)); }
                if (data.cell26 != null) { SetXlsxCellValue(srcSheet, rowIndex, 27, (float)Math.Round(Convert.ToSingle(data.cell26), 2)); }
                if (data.cell27 != null) { SetXlsxCellValue(srcSheet, rowIndex, 28, (float)Math.Round(Convert.ToSingle(data.cell27), 2)); }
                if (data.cell28 != null) { SetXlsxCellValue(srcSheet, rowIndex, 29, (float)Math.Round(Convert.ToSingle(data.cell28), 2)); }
                // ============ 以下 29-35条 已注释 (cell29-cell35) ============
                //if (data.cell29 != null) { SetXlsxCellValue(srcSheet, rowIndex, 30, (float)Math.Round(Convert.ToSingle(data.cell29), 2)); }
                //if (data.cell30 != null) { SetXlsxCellValue(srcSheet, rowIndex, 31, (float)Math.Round(Convert.ToSingle(data.cell30), 2)); }
                //if (data.cell31 != null) { SetXlsxCellValue(srcSheet, rowIndex, 32, (float)Math.Round(Convert.ToSingle(data.cell31), 2)); }
                //if (data.cell32 != null) { SetXlsxCellValue(srcSheet, rowIndex, 33, (float)Math.Round(Convert.ToSingle(data.cell32), 2)); }
                //if (data.cell33 != null) { SetXlsxCellValue(srcSheet, rowIndex, 34, (float)Math.Round(Convert.ToSingle(data.cell33), 2)); }
                //if (data.cell34 != null) { SetXlsxCellValue(srcSheet, rowIndex, 35, (float)Math.Round(Convert.ToSingle(data.cell34), 2)); }
                //if (data.cell35 != null) { SetXlsxCellValue(srcSheet, rowIndex, 36, (float)Math.Round(Convert.ToSingle(data.cell35), 2)); }
                // ============ 注释结束 继续写入后续数据 ============
                if (data.cell36 != null) { SetXlsxCellValue(srcSheet, rowIndex, 37, (float)Math.Round(Convert.ToSingle(data.cell36), 2)); }
                if (data.cell37 != null) { SetXlsxCellValue(srcSheet, rowIndex, 38, (float)Math.Round(Convert.ToSingle(data.cell37), 2)); }
                if (data.cell38 != null) { SetXlsxCellValue(srcSheet, rowIndex, 39, (float)Math.Round(Convert.ToSingle(data.cell38), 2)); }
                if (data.cell39 != null) { SetXlsxCellValue(srcSheet, rowIndex, 40, (float)Math.Round(Convert.ToSingle(data.cell39), 2)); }
                if (data.cell40 != null) { SetXlsxCellValue(srcSheet, rowIndex, 41, (float)Math.Round(Convert.ToSingle(data.cell40), 2)); }
                if (data.cell41 != null) { SetXlsxCellValue(srcSheet, rowIndex, 42, (float)Math.Round(Convert.ToSingle(data.cell41), 2)); }
                if (data.cell42 != null) { SetXlsxCellValue(srcSheet, rowIndex, 43, (float)Math.Round(Convert.ToSingle(data.cell42), 2)); }
            }
            return true;
        }

        /// <summary>
        /// 按区域写Xlsx数据 2
        /// </summary>
        private static bool WriteXlsxRange2(XSSFWorkbook srcWorkbook, XSSFWorkbook destWorkbook, ISheet srcSheet, string newSheetName)
        {
            return true;
        }
        /// <summary>
        /// 按区域写Xlsx数据 3
        /// </summary>
        private static bool WriteXlsxRange3(XSSFWorkbook srcWorkbook, XSSFWorkbook destWorkbook, ISheet srcSheet, string newSheetName)
        {
            return true;
        }
        /// <summary>
        /// 复制XLSX工作表（仅针对.xlsx，保留样式、合并单元格、列宽）
        /// </summary>
        private static ISheet CopyXlsxSheet3(XSSFWorkbook srcWorkbook, XSSFWorkbook destWorkbook, ISheet srcSheet, string newSheetName)
        {
            ISheet destSheet = destWorkbook.CreateSheet(newSheetName);

            // 1. 复制列宽（先获取模板表的最大列数，避免用srcSheet.LastCellNum）
            int maxColumnCount = 0;
            for (int rowIdx = 0; rowIdx <= srcSheet.LastRowNum; rowIdx++)
            {
                IRow srcRow = srcSheet.GetRow(rowIdx);
                if (srcRow != null && srcRow.LastCellNum > maxColumnCount)
                {
                    maxColumnCount = srcRow.LastCellNum; // 用IRow的LastCellNum
                }
            }
            for (int col = 0; col < maxColumnCount; col++)
            {
                destSheet.SetColumnWidth(col, srcSheet.GetColumnWidth(col));
            }

            // 2. 复制行和单元格（修复日期赋值错误）
            for (int rowIdx = 0; rowIdx <= srcSheet.LastRowNum; rowIdx++)
            {
                IRow srcRow = srcSheet.GetRow(rowIdx);
                IRow destRow = destSheet.CreateRow(rowIdx);

                if (srcRow != null)
                {
                    destRow.Height = srcRow.Height;

                    // 复制单元格（遍历行的LastCellNum）
                    for (int cellIdx = 0; cellIdx < srcRow.LastCellNum; cellIdx++)
                    {
                        ICell srcCell = srcRow.GetCell(cellIdx);
                        if (srcCell != null)
                        {
                            ICell destCell = destRow.CreateCell(cellIdx);
                            destCell.CellStyle = srcCell.CellStyle;
                            CopyCellValue(srcCell, destCell); // 修复日期赋值逻辑
                        }
                    }
                }
            }

            // 3. 复制合并单元格
            foreach (CellRangeAddress region in srcSheet.MergedRegions)
            {
                destSheet.AddMergedRegion(region);
            }

            return destSheet;
        }
        /// <summary>
        /// 复制单元格值（适配不同数据类型）
        /// </summary>
        private static void CopyCellValue(ICell srcCell, ICell destCell)
        {
            switch (srcCell.CellType)
            {
                case CellType.String:
                    destCell.SetCellValue(srcCell.StringCellValue);
                    break;
                case CellType.Numeric:
                    // 重点修复：显式处理日期类型，避免调用SetCellValue(double)
                    if (DateUtil.IsCellDateFormatted(srcCell))
                    {
                        // 处理可空DateTime：判断HasValue后取Value
                        DateTime? dateValue = srcCell.DateCellValue;
                        destCell.SetCellValue(dateValue.HasValue ? dateValue.Value : DateTime.MinValue);
                    }
                    else
                    {
                        destCell.SetCellValue(srcCell.NumericCellValue);
                    }
                    break;
                case CellType.Boolean:
                    destCell.SetCellValue(srcCell.BooleanCellValue);
                    break;
                case CellType.Formula:
                    destCell.SetCellFormula(srcCell.CellFormula);
                    break;
                default:
                    destCell.SetCellValue(srcCell.ToString());
                    break;
            }
        }

        /// <summary>
        /// 给XLSX单元格赋值（封装逻辑，简化调用）
        /// </summary>
        private static void SetXlsxCellValue(ISheet sheet, int rowIdx, int colIdx, float value)
        {
            // 获取或创建行
            IRow row = sheet.GetRow(rowIdx) ?? sheet.CreateRow(rowIdx);
            // 获取或创建单元格
            ICell cell = row.GetCell(colIdx) ?? row.CreateCell(colIdx);
            // 赋值
            cell.SetCellValue(value);
           
        }


    }


}
