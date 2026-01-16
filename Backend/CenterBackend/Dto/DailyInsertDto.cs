namespace CenterBackend.Dto
{
    //传入日期，根据日期计算每日数据并记录  
    public class DailyInsertDto
    {
        public DateTime AddDate { get; set; }
        public string? ReportType { get; set; }
    }
}
