using CenterBackend.Dto;
using CenterReport.Repository;
using CenterReport.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using static CenterBackend.Services.ReportService;

namespace CenterBackend.IReportServices
{
    public interface IReportService
    {

        Task<bool> DeleteReport(long id, AddReportDailyDto _AddReportDailyDto);
        Task<bool> AddReport(AddReportDailyDto _AddReportDailyDto);
        Task<bool> DailyCalculateAndInsertAsync(AddReportDailyDto _AddReportDailyDto);

        Task<IActionResult>  ExportReport();
    }
}
