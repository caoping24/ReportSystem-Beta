using CenterBackend.Dto;
using CenterReport.Repository.Models;
using Microsoft.AspNetCore.Mvc;

namespace CenterBackend.IReportServices
{
    public interface IReportService
    {

        //Task<bool> DeleteReport(CalculateAndInsertDto _CalculateAndInsertDto);
        //Task<bool> AddReport(CalculateAndInsertDto _CalculateAndInsertDto);
        Task<bool> DataAnalyses(CalculateAndInsertDto _Dto);
        Task<IActionResult> WriteXlsxAndSave(string ModelFullPath, string TargetPullPath, DateTime ReportTime, int Type);

        Task<List<CalculatedData>> getCalculatedData(DateTime time);

        Task<bool> UpdateCalculatedDataFieldAsync(string dateStr, int hour, string prop, string valueStr);
    }
}
