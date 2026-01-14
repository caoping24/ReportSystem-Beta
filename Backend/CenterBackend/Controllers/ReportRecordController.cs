using CenterBackend.common;
using CenterBackend.Constant;
using CenterBackend.Dto;
using CenterBackend.Exceptions;
using CenterBackend.IReportServices;
using CenterReport.Repository.Utils;
using CenterUser.Repository.Models;
using Masuit.Tools;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Text.Json;

namespace CenterBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportRecordController : ControllerBase
    {
        private readonly IReportRecordService _reportRecordService;

        public ReportRecordController(IReportRecordService reportRecordService)
        {
            this._reportRecordService = reportRecordService;
        }

        /// <summary>
        /// 分页记录列表
        /// </summary>
        /// <param name="request">分页参数</param>
        /// <returns>分页结果</returns>
        [HttpGet("GetReportByPage")]
        public async Task<ActionResult<PaginationResult<User>>> GetReportByPage([FromQuery] PaginationRequest request)
        {
            try
            {
                var result = await _reportRecordService.GetReportsByPageAsync(request);
                return Ok(result); // 返回200 + 分页结果
            }
            catch (Exception ex)
            {
                // 异常处理（实际项目可封装全局异常过滤器）
                return StatusCode(500, new { message = "查询失败", detail = ex.Message });
            }
        }

    }
}
