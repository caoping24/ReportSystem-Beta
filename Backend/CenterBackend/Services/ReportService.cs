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


        #region
        private async Task CalculateSourceData1Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            // cell1-cell10 完整赋值 - 按【平均值、差值、总和】循环重复，带注释+空值处理
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
        #endregion

        #region
        private async Task CalculateSourceData2Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和
        }
        #endregion

        #region
        private async Task CalculateSourceData3Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和
        }
        #endregion

        #region
        private async Task CalculateSourceData4Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和
        }
        #endregion

        #region
        private async Task CalculateSourceData5Async(DateTime StartTime, DateTime StopTime, HourlyDataStatistic target)
        {
            var dataList = await _sourceData1.GetByDataTimeAsync(StartTime, StopTime);

            target.cell1 = dataList.Select(x => x.cell1 ?? 0).Average();//平均值
            target.cell2 = (dataList.Last().cell2 ?? 0) - (dataList.First().cell2 ?? 0);//差值
            target.cell3 = dataList.Select(x => x.cell3 ?? 0).Sum();//总和
        }
        #endregion
    }


}
