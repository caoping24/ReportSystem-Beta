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
            List<SourceData> sourceDatas = await _reportRepository.GetByDayAsync(time);
            var fixedHours = Enumerable.Range(8, 16).Concat(Enumerable.Range(0, 9)).ToList();
            string[] xAxis = fixedHours.Select(h => $"{h:D2}").ToArray();
           
            if (sourceDatas == null || !sourceDatas.Any())
            {
                return new LineChartDataDto
                {
                    XAxis = xAxis,
                    Series = new List<LineChartSeriesDto>
            {
                new LineChartSeriesDto
                {
                    Name = "昨日",
                    // 替换NaN为null
                    Data = fixedHours.Select(_ => (double?)null).ToArray()
                }
            }
                };
            }
            double?[] data = new double?[sourceDatas.Count()];
            int index = 0;
            foreach (var item in sourceDatas)
            {
                data[index] = item.cell1;
                index++;
            }
            return new LineChartDataDto
            {
                XAxis = xAxis,
                Series = new List<LineChartSeriesDto>
                {
                    new LineChartSeriesDto
                    {
                        Name = "昨日",
                        Data = data
                    }
                }
            };
        }


        public async Task<LineChartDataDto> getLineCharTwo(DateTime time)
        {
            List<SourceData> sourceDatas = await _reportRepository.GetByDayAsync(time);
            var fixedHours = Enumerable.Range(8, 16).Concat(Enumerable.Range(0, 9)).ToList();
            string[] xAxis = fixedHours.Select(h => $"{h:D2}").ToArray();

            if (sourceDatas == null || !sourceDatas.Any())
            {
                return new LineChartDataDto
                {
                    XAxis = xAxis,
                    Series = new List<LineChartSeriesDto>
            {
                new LineChartSeriesDto
                {
                    Name = "昨日",
                    // 替换NaN为null
                    Data = fixedHours.Select(_ => (double?)null).ToArray()
                }
            }
                };
            }
            double?[] data = new double?[sourceDatas.Count()];
            int index = 0;
            foreach (var item in sourceDatas)
            {
                data[index] = item.cell2;
                index++;
            }
            return new LineChartDataDto
            {
                XAxis = xAxis,
                Series = new List<LineChartSeriesDto>
                {
                    new LineChartSeriesDto
                    {
                        Name = "昨日",
                        Data = data
                    }
                }
            };
        }

        public async Task<LineChartDataDto> getLineCharThree(DateTime time)
        {
            List<SourceData> sourceDatas = await _reportRepository.GetByDayAsync(time);
            var fixedHours = Enumerable.Range(8, 16).Concat(Enumerable.Range(0, 9)).ToList();
            string[] xAxis = fixedHours.Select(h => $"{h:D2}").ToArray();

            if (sourceDatas == null || !sourceDatas.Any())
            {
                return new LineChartDataDto
                {
                    XAxis = xAxis,
                    Series = new List<LineChartSeriesDto>
            {
                new LineChartSeriesDto
                {
                    Name = "昨日",
                    // 替换NaN为null
                    Data = fixedHours.Select(_ => (double?)null).ToArray()
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
                data2[index] = item.cell4;
                index++;
            }
            return new LineChartDataDto
            {
                XAxis = xAxis,
                Series = new List<LineChartSeriesDto>
                {
                    new LineChartSeriesDto
                    {
                        Name = "昨日",
                        Data = data1
                    },
                     new LineChartSeriesDto
                    {
                        Name = "今日",
                        Data = data2
                    }
                }
            };
        }




    }


}
