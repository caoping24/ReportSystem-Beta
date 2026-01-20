using CenterBackend.Dto;
using Microsoft.AspNetCore.Mvc;

namespace CenterBackend.IReportServices
{
    public interface IReportService
    {

        //Task<bool> DeleteReport(CalculateAndInsertDto _CalculateAndInsertDto);
        //Task<bool> AddReport(CalculateAndInsertDto _CalculateAndInsertDto);
        Task<bool>  DailyCalculateAndInsertAsync(CalculateAndInsertDto _Dto);
        Task<IActionResult> WriteXlsxAndSave_Daily(string ModelFullPath, string TargetPullPath, DateTime ReportTime);

    }
}
