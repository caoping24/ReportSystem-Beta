using CenterBackend.common;
using CenterBackend.Constant;
using CenterBackend.Dto;
using CenterBackend.Exceptions;
using CenterBackend.IReportServices;
using CenterReport.Repository;
using CenterReport.Repository.Models;
using Mapster;
using Masuit.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IReportRepository<SourceData4> _sourceData4;
        private readonly IReportRepository<SourceData5> _sourceData5;
        private readonly IReportRepository<HourlyDataStatistic> _hourlyDataStatistics;
        private readonly IReportUnitOfWork _reportUnitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // 构造函数注入：按顺序注入5个SourceData仓储 + 原有依赖，一一对应赋值
        public ReportService(IReportRepository<SourceData1> sourceData1,
                             IReportRepository<SourceData2> sourceData2,
                             IReportRepository<SourceData3> sourceData3,
                             IReportRepository<SourceData4> sourceData4,
                             IReportRepository<SourceData5> sourceData5,
                             IReportRepository<HourlyDataStatistic> hourlyDataStatistics,
                             IReportUnitOfWork reportUnitOfWork,
                             IHttpContextAccessor httpContextAccessor)
        {
            this._sourceData1 = sourceData1;
            this._sourceData2 = sourceData2;
            this._sourceData3 = sourceData3;
            this._sourceData4 = sourceData4;
            this._sourceData5 = sourceData5;
            this._hourlyDataStatistics = hourlyDataStatistics;
            this._reportUnitOfWork = reportUnitOfWork;
            this._httpContextAccessor = httpContextAccessor;
        }

        //根据id删除对应数据
        public async Task<bool> DeleteReport( long id, AddReportDailyDto _AddReportDailyDto)
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
        public async Task<bool> AddReport(AddReportDailyDto _AddReportDailyDto)
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
        public async Task<bool> DailyCalculateAndInsertAsync(AddReportDailyDto _AddReportDailyDto)
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
        /// ExportReport
        /// </summary>
        public async Task<IActionResult>  ExportReport()
        {
            try
            {
                // 1. 定义目标时间（以“当前时间减1天”为例，你可替换为实际需要的time）
                DateTime time = DateTime.Now.AddDays(-1);

                // 2. 异步获取指定日期的数据列表
                var dataList = await _sourceData1.GetByDayAsync(time);

                // 3. 空值检查：避免dataList为null导致遍历空引用异常
                if (dataList == null || !dataList.Any())
                {
                    Console.WriteLine("指定日期无数据，跳过处理");
                    throw new NotImplementedException();
                }

                //获取excel表格
                string templatePath = @"C:\Users\why\Desktop\报表1.xlsx"; // 替换为你的模板路径
                string outputPath = @"C:\Users\why\Desktop\excel\Report.xlsx";        // 替换为输出路径

                try { 
                    using FileStream templateStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
                    XSSFWorkbook workbook = new XSSFWorkbook(templateStream);
                    ISheet sheet = workbook.GetSheetAt(0); // 获取第一个工作表
                    // 假设从第2行开始写入数据（索引1），根据需要调整
                    int startRow = 5;
                    for (int i = 0; i < dataList.Count; i++)
                    {
                        var data = dataList[i];
                        int rowIndex = startRow + i;
                        // 写入cell1和cell2示例，按需添加更多字段
                        if (data.cell1 != null) {
                            SetXlsxCellValue(sheet, rowIndex, 3, (float)Math.Round(Convert.ToSingle(data.cell1), 2));
                        }
                        if (data.cell2 != null)
                        {
                            SetXlsxCellValue(sheet, rowIndex, 5, (float)Math.Round(Convert.ToSingle(data.cell1), 2));
                        }



                        // 继续为其他单元格赋值...
                    }
                    // 保存修改后的工作簿到新文件
                    using FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                    workbook.Write(outputStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理Excel时出错：{ex.Message}");
                    throw; // 可根据需要选择是否重新抛出异常
                }   
                
            }
            catch (Exception ex)
            {
                // 异常处理：捕获异步调用或类型转换中的错误
                Console.WriteLine($"处理数据时出错：{ex.Message}");
                // 可根据实际场景记录日志、抛出异常或返回错误状态
                // throw; // 如需向上层抛出异常，取消注释
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// 复制XLSX工作表（仅针对.xlsx，保留样式、合并单元格、列宽）
        /// </summary>
        private static ISheet CopyXlsxSheet(XSSFWorkbook srcWorkbook, XSSFWorkbook destWorkbook, ISheet srcSheet, string newSheetName)
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
