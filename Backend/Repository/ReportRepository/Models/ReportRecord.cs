using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CenterReport.Repository.Models
{
    [Table("ReportRecord")]//生成批量下载的列表 type: 1=日报表 2=周报表 3=年报表
    public class ReportRecord
    {
        public long Id { get; set; }
        public DateTime createdtime { get; set; } = DateTime.Now;
        public int Type { get; set; }
    }

}

