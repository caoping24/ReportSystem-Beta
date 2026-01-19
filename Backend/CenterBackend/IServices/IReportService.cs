using CenterBackend.Dto;
using Microsoft.AspNetCore.Mvc;

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
