using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace CenterReport.Repository.Models
{

    [Table("ReportRecord")]
    public class ReportRecord
    {
   
        public long Id { get; set; }
        public DateTime createdtime { get; set; }
        public int Type { get; set; }
    
    }

}

