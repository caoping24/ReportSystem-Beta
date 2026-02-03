using CenterBackend.Dto;
using CenterBackend.IReportServices;
using CenterBackend.IServices;
using CenterReport.Repository;
using CenterReport.Repository.Models;
using CenterReport.Repository.Utils;

namespace CenterBackend.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IReportRepository<SourceData> _reportRepository;

        public DashboardService(IReportRepository<SourceData> _reportRepository)
        {
            this._reportRepository = _reportRepository;
        }
        public async Task<LineChartDataDto> getLineChartOne(DateTime time)
        {
            List<SourceData> sourceDatas = await _reportRepository.GetByDataTimeAsync(time.AddHours(-24), time);
            string[] xAxis = Enumerable.Range(0, 24)
                           .Select(n => time.AddHours(-23 + n))
                           .Select(t => t.Hour.ToString("D2"))
                           .ToArray();

            if (sourceDatas == null || !sourceDatas.Any())
            {
                return new LineChartDataDto
                {
                    XAxis = xAxis,
                    Series = new List<LineChartSeriesDto>
                    {
                        new LineChartSeriesDto
                        {
                            Name = "无数据",
                            // 替换NaN为null
                            Data = Enumerable.Range(0,24).Select(_ => (double?)null).ToArray() // 改为24个null，匹配X轴
                        }
                    }
                };
            }
            double?[] data = new double?[sourceDatas.Count()];
            int index = 0;
            foreach (var item in sourceDatas)
            {
                data[index] = item.cell19;
                index++;
            }
            return new LineChartDataDto
            {
                XAxis = xAxis,
                Series = new List<LineChartSeriesDto>
                {
                    new LineChartSeriesDto
                    {
                        Name = "羟基乙睛进料流量",
                        Data = data
                    }
                }
            };
        }


        public async Task<LineChartDataDto> getLineCharTwo(DateTime time)
        {
            List<SourceData> sourceDatas = await _reportRepository.GetByDataTimeAsync(time.AddHours(-24), time);
            string[] xAxis = Enumerable.Range(0, 24)
                           .Select(n => time.AddHours(-23 + n))
                           .Select(t => t.Hour.ToString("D2"))
                           .ToArray();

            if (sourceDatas == null || !sourceDatas.Any())
            {
                return new LineChartDataDto
                {
                    XAxis = xAxis,
                    Series = new List<LineChartSeriesDto>
                    {
                        new LineChartSeriesDto
                        {
                            Name = "无数据",
                            // 替换NaN为null
                            Data = Enumerable.Range(0,24).Select(_ => (double?)null).ToArray() // 改为24个null，匹配X轴
                        }
                    }
                };
            }
            double?[] data = new double?[sourceDatas.Count()];
            int index = 0;
            foreach (var item in sourceDatas)
            {
                data[index] = item.cell22;
                index++;
            }
            return new LineChartDataDto
            {
                XAxis = xAxis,
                Series = new List<LineChartSeriesDto>
                {
                    new LineChartSeriesDto
                    {
                        Name = "摩尔比",
                        Data = data
                    }
                }
            };
        }

        public async Task<LineChartDataDto> getLineCharThree(DateTime time)
        {
            List<SourceData> sourceDatas = await _reportRepository.GetByDataTimeAsync(time.AddHours(-24), time);
            string[] xAxis = Enumerable.Range(0, 24)
                           .Select(n => time.AddHours(-23 + n))
                           .Select(t => t.Hour.ToString("D2"))
                           .ToArray();

            if (sourceDatas == null || !sourceDatas.Any())
            {
                return new LineChartDataDto
                {
                    XAxis = xAxis,
                    Series = new List<LineChartSeriesDto>
            {
                new LineChartSeriesDto
                {
                    Name = "无数据",
                    Data = Enumerable.Range(0,24).Select(_ => (double?)null).ToArray() // 改为24个null，匹配X轴
                }
            }
                };
            }
            double?[] data1 = new double?[sourceDatas.Count()];
            double?[] data2 = new double?[sourceDatas.Count()];
            int index = 0;
            foreach (var item in sourceDatas)
            {
                data1[index] = item.cell3;
                data2[index] = item.cell6;
                index++;
            }
            return new LineChartDataDto
            {
                XAxis = xAxis,
                Series = new List<LineChartSeriesDto>
                {
                    new LineChartSeriesDto
                    {
                        Name = "羟基原料浓度1",
                        Data = data1
                    },
                     new LineChartSeriesDto
                    {
                        Name = "羟基配后浓度2",
                        Data = data2
                    }
                }
            };
        }

        public async Task<List<PieChartItemDto>> getPieChart(DateTime time)
        {
            await Task.Delay(1);
            var pieChartItems = new List<PieChartItemDto>
        {
            new PieChartItemDto { Name = "类别A", Value = 40 },
            new PieChartItemDto { Name = "类别B", Value = 30 },
            new PieChartItemDto { Name = "类别C", Value = 20 },
            new PieChartItemDto { Name = "类别D", Value = 10 }
        };

            return pieChartItems;
        }

        public async Task<CoreChartDto>  getCoreChart(DateTime time)
        {
            await Task.Delay(1);
            var coreChartDto = new CoreChartDto
            {
                Yesterday=55.23,
                Week=66.22,
                Month=21.21,
                Year=12.33
            };
            return coreChartDto;
        }
    }


}
