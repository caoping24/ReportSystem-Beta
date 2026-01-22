using CenterBackend.Dto;
using CenterBackend.IReportServices;
using CenterReport.Repository;
using CenterReport.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace CenterBackend.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository<SourceData> _sourceData;
        private readonly IReportRecordRepository<ReportRecord> _reportRecord;
        private readonly IReportRepository<CalculatedData> _calculatedDatas;
        private readonly IReportUnitOfWork _reportUnitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // 构造函数注入：按顺序注入5个SourceData仓储 + 原有依赖，一一对应赋值
        public ReportService(IReportRepository<SourceData> SourceData,

                             IReportRecordRepository<ReportRecord> reportRecord,
                             IReportRepository<CalculatedData> CalculatedDatas,
                             IReportUnitOfWork reportUnitOfWork,
                             IHttpContextAccessor httpContextAccessor)
        {
            this._sourceData = SourceData;
            this._reportRecord = reportRecord;
            this._calculatedDatas = CalculatedDatas;
            this._reportUnitOfWork = reportUnitOfWork;
            this._httpContextAccessor = httpContextAccessor;
        }


        //public async Task<bool> DeleteReport(long id, DailyInsertDto _AddReportDailyDto)
        //{
        //    return true;
        //}

        //public async Task<bool> AddReport(DailyInsertDto _AddReportDailyDto)
        //{
        //    return true;
        //}


        /// <summary>
        /// 每日统计数据
        /// </summary>
        public async Task<bool> DataAnalyses(CalculateAndInsertDto _Dto)
        {
            DateTime StartTime;
            DateTime StopTime;

            switch (_Dto.Type)
            {
                case 1: // 当天
                    StartTime = _Dto.Time.Date.AddDays(-1).AddHours(8); // 开始时间等于昨天的8点0分
                    StopTime = StartTime.AddHours(24).AddMinutes(59); // 结束时间等于昨天的20点59分
                    break;
                case 2: // 上周
                    DateTime currentDayOfWeek = _Dto.Time.Date;// 计算上周的开始时间（星期一）
                    int daysToLastMonday = ((int)currentDayOfWeek.DayOfWeek + 6) % 7 + 7;
                    StartTime = currentDayOfWeek.AddDays(-daysToLastMonday);
                    StopTime = StartTime.AddDays(6).AddHours(23).AddMinutes(59);// 计算上周的结束时间（星期天）
                    break;
                case 3: // 上月
                    StartTime = new DateTime(_Dto.Time.Year, _Dto.Time.Month, 1).AddMonths(-1);// 计算上月的开始时间（1号）
                    StopTime = StartTime.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);// 计算上月的结束时间（最后一天）
                    break;
                case 4: // 去年   
                    StartTime = new DateTime(_Dto.Time.Year, 1, 1).AddYears(-1);// 计算去年的开始时间（1月1号）
                    StopTime = new DateTime(_Dto.Time.Year, 1, 1).AddDays(-1).AddHours(23).AddMinutes(59);// 计算去年的结束时间（12月31号）
                    break;
                default:
                    return false;
            }
            CalculatedData targetModel = new CalculatedData();
            return await CalculatedDataAndInsert(StartTime, StopTime, targetModel, _Dto.Type);
        }
        /// <summary>
        /// 根据Tpye类型，计算周/月/年统计数据
        /// </summary>
        private async Task<bool> CalculatedDataAndInsert(DateTime StartTime, DateTime StopTime, CalculatedData target, int Type)
        {
            if (Type == 1)
            {
                var dataList1 = await _sourceData.GetByDataTimeAsync(StartTime, StopTime);//查询日数据
                if (dataList1.Count == 0) return false;
                await DayDataCalculate_1(target, dataList1);
            }
            else
            {
                List<CalculatedData> dataList;
                switch (Type)
                {
                    case 2://周
                        dataList = await _calculatedDatas.GetByDataTimeAsync(StartTime, StopTime, 1);//查询日数据
                        if (dataList.Count == 0) return false;
                        await WeekDataCalculate(target, dataList);
                        break;
                    case 3://月
                        dataList = await _calculatedDatas.GetByDataTimeAsync(StartTime, StopTime, 1);//查询日数据
                        if (dataList.Count == 0) return false;
                        await MonthDataCalculate(target, dataList);
                        break;
                    case 4://年
                        dataList = await _calculatedDatas.GetByDataTimeAsync(StartTime, StopTime, 3);//查询月数据
                        if (dataList.Count == 0) return false;
                        await YearDataCalculate(target, dataList);
                        break;
                    default:
                        break;
                }
            }
            await _calculatedDatas.AddAsync(target);
            await _reportUnitOfWork.SaveChangesAsync();
            return true;
        }
        private async Task DayDataCalculate_1(CalculatedData target, List<SourceData> dataList)
        {
            target.Type = 1;//标记该数据是日统计数据
            target.PH = 80;//暂时没有特殊意义

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
        private async Task WeekDataCalculate(CalculatedData target, List<CalculatedData> dataList)
        {
            target.Type = 2;//标记该数据是月统计数据
            target.PH = 80;//暂时没有特殊意义

            //根据实际情况处理
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
        private async Task MonthDataCalculate(CalculatedData target, List<CalculatedData> dataList)
        {
            target.Type = 3;//标记该数据是月统计数据
            target.PH = 80;//暂时没有特殊意义

            //根据实际情况处理
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
        private async Task YearDataCalculate(CalculatedData target, List<CalculatedData> dataList)
        {
            target.Type = 4;//标记该数据是月统计数据
            target.PH = 80;//暂时没有特殊意义

            //根据实际情况处理
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

        /// <summary>
        /// 从模板文件创建文件流，然后按区域写数据并且保存到本地文件 Daily
        /// </summary>
        /// <param name="ModelFullPath">模板完整路径</param>
        /// <param name="TargetPullPath">生成文件的保存路径</param>
        /// <param name="ReportTime">报表日期</param>
        /// <returns></returns>
        public async Task<IActionResult> WriteXlsxAndSave(string ModelFullPath, string TargetPullPath, DateTime ReportTime, int Type)
        {
            DateTime StartTime;
            DateTime StopTime;
            List<SourceData> dataList;
            List<SourceData> Temp_DataList;
            List<CalculatedData> dataList2;
            try
            {
                using var templateStream = new FileStream(ModelFullPath, FileMode.Open, FileAccess.Read);
                using var workbook = new XSSFWorkbook(templateStream);

                switch (Type)
                {
                    case 1: //当日8点以后查询昨日

                        if (ReportTime.AddMinutes(-1).Hour < 8)
                        {
                            return new OkObjectResult(new { success = false, msg = $"类型:{Type} 时间:{ReportTime:yyyy-MM-dd hh:mm:ss} 应大于8:00" });
                        }
                        dataList = await _sourceData.GetByDayAsync(ReportTime);
                        if (dataList == null || !dataList.Any())
                        {

                            return new OkObjectResult(new { success = false, msg = $"类型:{Type} 时间:{ReportTime:yyyy-MM-dd hh:mm:ss} 无数据" });
                        }
                        Temp_DataList = dataList.Where(x => x.Type == 1).ToList();
                        WriteXlsxDaily1(workbook, 5, dataList);

                        Temp_DataList = dataList.Where(x => x.Type == 2).ToList();
                        WriteXlsxDaily2(workbook, 5, dataList);
                        break;
                    case 2: // 上周
                        DateTime currentDayOfWeek = ReportTime.Date;// 计算上周的开始时间（星期一）
                        int daysToLastMonday = ((int)currentDayOfWeek.DayOfWeek + 6) % 7 + 7;
                        StartTime = currentDayOfWeek.AddDays(-daysToLastMonday);
                        StopTime = StartTime.AddDays(6).AddHours(23).AddMinutes(59);// 计算上周的结束时间（星期天）

                        dataList2 = await _calculatedDatas.GetByDataTimeAsync(StartTime, StopTime, 2);
                        if (dataList2 == null || !dataList2.Any())
                        {
                            return new OkObjectResult(new { success = false, msg = $"类型:{Type} 时间:{ReportTime:yyyy-MM-dd hh:mm:ss} 无数据" });
                        }
                        WriteXlsxWeekly(workbook, 5, dataList2);
                        break;
                    case 3: // 上月
                        StartTime = new DateTime(ReportTime.Year, ReportTime.Month, 1).AddMonths(-1);// 计算上月的开始时间（1号）
                        StopTime = StartTime.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);// 计算上月的结束时间（最后一天）
                        dataList2 = await _calculatedDatas.GetByDataTimeAsync(StartTime, StopTime, 3);
                        if (dataList2 == null || !dataList2.Any())
                        {
                            return new OkObjectResult(new { success = false, msg = $"类型:{Type} 时间:{ReportTime:yyyy-MM-dd hh:mm:ss} 无数据" });
                        }
                        WriteXlsxMonthly(workbook, 5, dataList2);
                        break;
                    case 4: // 去年   
                        StartTime = new DateTime(ReportTime.Year, 1, 1).AddYears(-1);// 计算去年的开始时间（1月1号）
                        StopTime = new DateTime(ReportTime.Year, 1, 1).AddDays(-1).AddHours(23).AddMinutes(59);// 计算去年的结束时间（12月31号）
                        dataList2 = await _calculatedDatas.GetByDataTimeAsync(StartTime, StopTime, 4);
                        if (dataList2 == null || !dataList2.Any())
                        {
                            return new OkObjectResult(new { success = false, msg = $"类型:{Type} 时间:{ReportTime:yyyy-MM-dd hh:mm:ss} 无数据" });
                        }
                        WriteXlsxYearly(workbook, 5, dataList2);
                        break;
                    default:
                        return new OkObjectResult(new { success = false, msg = $"类型:{Type} 时间:{ReportTime:yyyy-MM-dd hh:mm:ss} 类型无效" });
                }

                using var outputStream = new FileStream(TargetPullPath, FileMode.Create, FileAccess.Write);// 保存文件到指定路径
                workbook.Write(outputStream);

                var temp = new ReportRecord();
                temp.Type = 1;
                await _reportRecord.AddAsync(temp);
                await _reportUnitOfWork.SaveChangesAsync();
                return new OkObjectResult(new { success = true, msg = $"类型:{Type} 时间:{ReportTime:yyyy-MM-dd hh:mm:ss} Excel生成成功" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, msg = $"生成Excel异常:类型:{Type}, 时间:{ReportTime:yyyy-MM-dd hh:mm:ss} ，异常信息：{ex}" });
            }
        }

        /// <summary>
        /// 写Xlsx数据  白班
        /// </summary>
        private static bool WriteXlsxDaily1(XSSFWorkbook srcWorkbook, int startRow, IEnumerable<SourceData> dataList)
        {
            ISheet srcSheet = srcWorkbook.GetSheetAt(0); //实际要写的表
            srcSheet.ForceFormulaRecalculation = false;//批量写入关闭公式自动计算，大幅提升写入速度
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
        /// 写Xlsx数据  夜班
        /// </summary>
        private static bool WriteXlsxDaily2(XSSFWorkbook srcWorkbook, int startRow, IEnumerable<SourceData> dataList)
        {
            ISheet srcSheet = srcWorkbook.GetSheetAt(1); //实际要写的表
            srcSheet.ForceFormulaRecalculation = false;//批量写入关闭公式自动计算，大幅提升写入速度
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
        /// 写Xlsx数据  上周
        /// </summary>
        private static bool WriteXlsxWeekly(XSSFWorkbook srcWorkbook, int startRow, IEnumerable<CalculatedData> dataList)
        {
            ISheet srcSheet = srcWorkbook.GetSheetAt(0); //实际要写的表
            srcSheet.ForceFormulaRecalculation = false;//批量写入关闭公式自动计算，大幅提升写入速度
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
        /// 写Xlsx数据  上月
        /// </summary>
        private static bool WriteXlsxMonthly(XSSFWorkbook srcWorkbook, int startRow, IEnumerable<CalculatedData> dataList)
        {
            ISheet srcSheet = srcWorkbook.GetSheetAt(0); //实际要写的表
            srcSheet.ForceFormulaRecalculation = false;//批量写入关闭公式自动计算，大幅提升写入速度
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
        /// 写Xlsx数据  去年年
        /// </summary>
        private static bool WriteXlsxYearly(XSSFWorkbook srcWorkbook, int startRow, IEnumerable<CalculatedData> dataList)
        {
            ISheet srcSheet = srcWorkbook.GetSheetAt(0); //实际要写的表
            srcSheet.ForceFormulaRecalculation = false;//批量写入关闭公式自动计算，大幅提升写入速度
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
