using CenterReport.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace CenterReport.Repository
{
    public class CenterReportDbContext : DbContext
    {

        public DbSet<SourceData1> SourceData1 => Set<SourceData1>();
        public DbSet<SourceData2> SourceData2 => Set<SourceData2>();
        public DbSet<SourceData3> SourceData3 => Set<SourceData3>();
        public DbSet<SourceData4> SourceData4 => Set<SourceData4>();
        public DbSet<SourceData5> SourceData5 => Set<SourceData5>();

        public DbSet<HourlyDataStatistic> HourlyDataStatistics => Set<HourlyDataStatistic>();

        public DbSet<ReportRecord> ReportRecord => Set<ReportRecord>();
        public CenterReportDbContext(DbContextOptions<CenterReportDbContext> options)
               : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
