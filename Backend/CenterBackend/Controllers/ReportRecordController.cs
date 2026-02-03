using AngleSharp.Io;
using CenterBackend.Dto;
using CenterBackend.IReportServices;
using CenterBackend.Services;
using CenterReport.Repository.Models;
using CenterReport.Repository.Utils;
using CenterUser.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CenterBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportRecordController : ControllerBase
    {
        private readonly IReportRecordService _reportRecordService;
        private readonly IReportService reportService;
        public ReportRecordController(IReportRecordService reportRecordService, IReportService reportService)
        {
            this._reportRecordService = reportRecordService;
            this.reportService = reportService;
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
            // 1. 校验日期格式
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var queryDate))
            {
                return BadRequest(new { message = "日期格式错误，请传入YYYY-MM-DD格式" });
            }

            try
            {
                //定义时间范围（和仓储层一致），用于区分数据属于「今日」还是「次日」
                DateTime startTime = queryDate.Date.AddHours(8);   // 今日8点（查询开始）
                DateTime endTime = startTime.AddDays(1);          // 次日8点

                // 2. 获取该时间段内的所有CalculatedData数据
                var calculatedDatas = await reportService.getCalculatedData(queryDate);

                // 3.构建【带日期维度的分组键】，区分今日8点和次日8点
                var dataWithKey = calculatedDatas.Select(cd => new
                {
                    Data = cd,
                    GroupKey = cd.createdtime >= startTime && cd.createdtime < queryDate.Date.AddDays(1)
                        ? cd.createdtime.Hour          
                        : cd.createdtime.Hour + 100    
                }).ToList();

                // 4. 按唯一分组键分组
                var hourGroupDict = dataWithKey
                    .GroupBy(item => item.GroupKey)
                    .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.Data); // 按唯一键取第一条数据

                var hourList = new List<int> { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 0, 1, 2, 3, 4, 5, 6, 7, 8 };

                // 6. 构建返回数据
                var hourDataList = hourList.Select((hour, index) =>
                {
                    // 【修改1】初始化HourDataDto时，先不赋值IsNextDay（需先判断是否有数据）
                    var hourData = new HourDataDto
                    {
                        Hour = hour,
                        Date = date
                        // 移除原IsNextDay = index >=16，延后赋值
                    };

                    // 根据是否是原次日时段，匹配分组键
                    int targetKey = index >= 16 ? hour + 100 : hour;
                    // 从分组中获取对应数据（无数据则targetData为null）
                    hourGroupDict.TryGetValue(targetKey, out var targetData);

                    // 【核心修改2】IsNextDay双条件判断：
                    // 条件1：原逻辑（索引>=16为次日时段）；条件2：分组中无对应数据
                    // 两个条件满足其一，即置为true
                    hourData.IsNextDay = index >= 16 || targetData == null;

                    // 7. 填充cell字段（逻辑不变，无数据为空字符串）
                    hourData.Cells["cell29"] = targetData?.cell29?.ToString("0.00") ?? "";
                    hourData.Cells["cell30"] = targetData?.cell30?.ToString("0.00") ?? "";
                    hourData.Cells["cell31"] = targetData?.cell31?.ToString("0.00") ?? "";
                    hourData.Cells["cell32"] = targetData?.cell32?.ToString("0.00") ?? "";
                    hourData.Cells["cell33"] = targetData?.cell33?.ToString("0.00") ?? "";
                    hourData.Cells["cell34"] = targetData?.cell34?.ToString("0.00") ?? "";
                    hourData.Cells["cell35"] = targetData?.cell35?.ToString("0.00") ?? "";
                    hourData.Cells["cell56"] = targetData?.cell56?.ToString("0.00") ?? "";
                    hourData.Cells["cell57"] = targetData?.cell57?.ToString("0.00") ?? "";
                    hourData.Cells["cell58"] = targetData?.cell58?.ToString("0.00") ?? "";
                    hourData.Cells["cell59"] = targetData?.cell59?.ToString("0.00") ?? "";
                    hourData.Cells["cell60"] = targetData?.cell60?.ToString("0.00") ?? "";
                    hourData.Cells["cell82"] = targetData?.cell82?.ToString("0.00") ?? "";
                    hourData.Cells["cell83"] = targetData?.cell83?.ToString("0.00") ?? "";
                    hourData.Cells["cell84"] = targetData?.cell84?.ToString("0.00") ?? "";
                    hourData.Cells["cell85"] = targetData?.cell85?.ToString("0.00") ?? "";
                    hourData.Cells["cell86"] = targetData?.cell86?.ToString("0.00") ?? "";
                    hourData.Cells["cell87"] = targetData?.cell87?.ToString("0.00") ?? "";
                    hourData.Cells["cell135"] = targetData?.cell135?.ToString("0.00") ?? "";
                    hourData.Cells["cell136"] = targetData?.cell136?.ToString("0.00") ?? "";
                    hourData.Cells["cell137"] = targetData?.cell137?.ToString("0.00") ?? "";
                    hourData.Cells["cell138"] = targetData?.cell138?.ToString("0.00") ?? "";
                    hourData.Cells["cell139"] = targetData?.cell139?.ToString("0.00") ?? "";
                    hourData.Cells["cell140"] = targetData?.cell140?.ToString("0.00") ?? "";
                    hourData.Cells["cell141"] = targetData?.cell141?.ToString("0.00") ?? "";

                    return hourData;
                }).ToList();

                return Ok(hourDataList);
            }
            catch (Exception ex)
            {
                // 建议补充日志记录（生产环境必备）
                // _logger.LogError(ex, "查询小时数据失败，日期：{QueryDate}", date);
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
                await reportService.UpdateCalculatedDataFieldAsync(
                        dateStr: request.Date,
                        hour: request.Hour,
                        prop: request.Prop,
                        valueStr: request.Value);

                return Ok(); // 返回200 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "查询失败", detail = ex.Message });
            }
        }
    
    
    }
}
