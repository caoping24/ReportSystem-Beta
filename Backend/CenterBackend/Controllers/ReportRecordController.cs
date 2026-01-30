using AngleSharp.Io;
using CenterBackend.Dto;
using CenterBackend.IReportServices;
using CenterReport.Repository.Models;
using CenterReport.Repository.Utils;
using CenterUser.Repository.Models;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<ActionResult<PaginationResult<ReportRecord>>> GetReportByPage([FromQuery] PaginationRequest request)
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
       
        private readonly List<TableHeaderDto> _mockHeaders = new()
        {
            new TableHeaderDto { Prop = "hour", Label = "小时" },
            new TableHeaderDto { Prop = "cell29", Label = "二乙腈含量-化分（%）" },
            new TableHeaderDto { Prop = "cell30", Label = "二乙腈含量-色谱（%）" },
            new TableHeaderDto { Prop = "cell31", Label = "羟基乙腈残余（%）" },
            new TableHeaderDto { Prop = "cell32", Label = "羟基乙腈残余（g/L）" },
            new TableHeaderDto { Prop = "cell33", Label = "甘氨腈（g/L）" },
            new TableHeaderDto { Prop = "cell34", Label = "三乙腈（g/L）" },
            new TableHeaderDto { Prop = "cell35", Label = "反应液检测数据pH" },
            new TableHeaderDto { Prop = "cell56", Label = "COD(mg/L)" },
            new TableHeaderDto { Prop = "cell57", Label = "TCN/总腈(mg/L)" },
            new TableHeaderDto { Prop = "cell58", Label = "NH3-N氨氮(mg/L)" },
            new TableHeaderDto { Prop = "cell59", Label = "HCHO甲醛(mg/L)" },
            new TableHeaderDto { Prop = "cell60", Label = "闪发器冷凝液ph" },
            new TableHeaderDto { Prop = "cell82", Label = "一次分离产量（Kg）" },
            new TableHeaderDto { Prop = "cell83", Label = "二乙腈含量化分（%）" },
            new TableHeaderDto { Prop = "cell84", Label = "二乙腈含量色谱（%）" },
            new TableHeaderDto { Prop = "cell85", Label = "羟基乙腈残余（%）" },
            new TableHeaderDto { Prop = "cell86", Label = "羟基乙腈残余（g/L）" },
            new TableHeaderDto { Prop = "cell87", Label = "硫铵含量（g/L）" },
            new TableHeaderDto { Prop = "cell135", Label = "脱色前透光率(%)" },
            new TableHeaderDto { Prop = "cell136", Label = "脱色后透光率(%)" },
            new TableHeaderDto { Prop = "cell137", Label = "脱色输送泵（mbar）" },
            new TableHeaderDto { Prop = "cell138", Label = "低蒸出料泵（mbar）" },
            new TableHeaderDto { Prop = "cell139", Label = "低蒸循环泵（mbar）" },
            new TableHeaderDto { Prop = "cell140", Label = "一次结晶清洗泵（mbar）" },
            new TableHeaderDto { Prop = "cell141", Label = "二次结晶清洗泵（mbar）" }
           
        };
        [HttpGet("Headers")]
        public async Task<ActionResult<List<TableHeaderDto>>> GetHeaders() {
            
            try
            {
                return Ok(_mockHeaders); // 返回200 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "查询失败", detail = ex.Message });
            }
        }

        [HttpGet("HourData")]
        public async Task<ActionResult<List<HourDataDto>>> GetHourData([FromQuery] string date)
        {
            // 校验日期格式
            if (!DateTime.TryParse(date, out var queryDate))
            {
                return StatusCode(500, new { message = "日期格式错误，请传入YYYY-MM-DD格式" });
            }
            try
            {
                // 小时列表
                var hourList = new List<int> { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 0, 1, 2, 3, 4, 5, 6, 7, 8 };
                var random = new Random();
                var mockData = hourList.Select((hour, index) =>
                {
                    var hourData = new HourDataDto
                    {
                        Hour = hour,
                        Date = date,
                        IsNextDay = index >= 16 // 索引16及以后标记为次日
                    };

                    // 填充数据
                    hourData.Cells[$"cell29"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell30"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell31"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell32"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell33"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell34"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell35"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell56"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell57"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell58"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell59"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell60"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell82"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell83"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell84"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell85"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell86"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell87"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell135"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell136"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell137"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell138"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell139"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell140"] = (random.NextDouble() * 100).ToString("0.0");
                    hourData.Cells[$"cell141"] = (random.NextDouble() * 100).ToString("0.0");

                    return hourData;
                }).ToList();
                return Ok(mockData); // 返回200 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "查询失败", detail = ex.Message });
            }
        }

        [HttpPost("SaveCell")]
        public async Task<ActionResult<List<TableHeaderDto>>> SaveCell([FromBody] SaveCellRequestDto request) {
            // 校验必填参数
            if (string.IsNullOrEmpty(request.Date)
                || string.IsNullOrEmpty(request.Prop)
                || request.Hour < 0 || request.Hour > 23)
            {
                return StatusCode(500, new { message = "参数不合法" });  
            }
            try
            {
                return Ok(); // 返回200 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "查询失败", detail = ex.Message });
            }
        }
    }
}
