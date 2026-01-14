using CenterBackend.common;
using CenterBackend.Constant;
using CenterBackend.Dto;
using CenterBackend.Exceptions;
using CenterBackend.IReportServices;
using CenterReport.Repository;
using CenterReport.Repository.Models;
using CenterReport.Repository.Utils;
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
    public class ReportRecordService : IReportRecordService
    {
        private readonly IReportRecordRepository<ReportRecord> _reportRecord;


        public ReportRecordService(IReportRecordRepository<ReportRecord> reportRecord)
        {
            this._reportRecord = reportRecord;
        }
           

        public async Task<PaginationResult<ReportRecord>> GetReportsByPageAsync(PaginationRequest request)
        {
            return await _reportRecord.GetReportByPageAsync(request);
        }
    }


}
