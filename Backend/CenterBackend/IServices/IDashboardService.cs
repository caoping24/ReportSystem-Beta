using CenterBackend.Dto;

namespace CenterBackend.IServices
{
    public interface IDashboardService
    {
        //第一个折线图
       Task<LineChartDataDto> getLineChartOne(DateTime time);
    }
}
