using CenterBackend.Dto;
using CenterReport.Repository;
using CenterReport.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using static CenterBackend.Services.ReportService;

namespace CenterBackend.IReportServices
{
    public interface IReportService
    {

        Task<bool> DeleteReport(long id, DailyInsertDto _AddReportDailyDto);
        Task<bool> AddReport(DailyInsertDto _AddReportDailyDto);
        Task<bool> DailyCalculateAndInsertAsync(DailyInsertDto _AddReportDailyDto);
        Task<IActionResult> WriteXlsxAndSave(string ModelFullPath, string TargetPullPath, DateTime ReportTime);
    }
}
