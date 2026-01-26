namespace CenterBackend.Dto
{
    public class CoreChartDto
    {
        /// <summary>
        /// 昨日数据
        /// </summary>
        public double Yesterday { get; set; }

        /// <summary>
        /// 本周数据
        /// </summary>
        public double Week { get; set; }

        /// <summary>
        /// 本月数据
        /// </summary>
        public double Month { get; set; }

        /// <summary>
        /// 本年数据
        /// </summary>
        public double Year { get; set; }
    }
}
