using CenterReport.Repository.Models;
using CenterReport.Repository.Utils;
using Microsoft.EntityFrameworkCore;

namespace CenterReport.Repository
{
    public class ReportRecordRepository<T> : IReportRecordRepository<T> where T : class
    {
        protected readonly CenterReportDbContext _context;

        public ReportRecordRepository(CenterReportDbContext context)
        {
            _context = context;
          
        }

        public IQueryable<T> db => throw new NotImplementedException();

        public async Task AddAsync(T entity)
        {
            await _context.AddAsync(entity);
        }

        public async  Task<PaginationResult<ReportRecord>> GetReportByPageAsync(PaginationRequest request)
        {
            // 校验分页参数（避免无效参数）
            var pageIndex = Math.Max(1, request.PageIndex);
            var pageSize = Math.Clamp(request.PageSize, 1, 100); // 限制每页最大条数为100

            // 核心分页逻辑：先查总条数，再查当前页数据
            var query = _context.ReportRecord
                                  .AsNoTracking() // 只读场景提升性能
                                  .OrderByDescending(r => r.createdtime);// AsNoTracking提升查询性能（只读场景）
            var totalCount = await query.LongCountAsync(); // 总记录数
            var data = await query
                .Skip((pageIndex - 1) * pageSize) // 跳过前面的记录
                .Take(pageSize) // 取当前页的记录
                .ToListAsync();

            // 构造分页结果返回
            return new PaginationResult<ReportRecord>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = data
            };
        }
    }
}
