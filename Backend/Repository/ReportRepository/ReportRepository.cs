using Microsoft.EntityFrameworkCore;

namespace CenterReport.Repository
{
    public class ReportRepository<T> : IReportRepository<T> where T : class
    {
        protected readonly CenterReportDbContext _context;
        private DbSet<T> _entities;

        public ReportRepository(CenterReportDbContext context)
        {
            _context = context;
            _entities = _context.Set<T>();
        }

        public IQueryable<T> db => _entities.AsQueryable();

        public async Task<List<T>> GetByDataTimeAsync(DateTime dateTime)
        {
            return await GetByDataTimeAsync(dateTime, dateTime);
        }
        public async Task<List<T>> GetByDataTimeAsync(DateTime start, DateTime end)
        {
            var from = DateTime.Compare(start, end) > 0 ? end : start;
            var to = DateTime.Compare(start, end) > 0 ? start : end;

            if (from == to)//获取满足条件的最新的一条记录
            {
                return await _entities
                    .Where(e => EF.Property<DateTime>(e, "createdtime") == from)
                    .ToListAsync();
            }
            return await _entities
                .Where(e => EF.Property<DateTime>(e, "createdtime") >= from && EF.Property<DateTime>(e, "createdtime") <= to)
                .OrderBy(e => EF.Property<DateTime>(e, "createdtime"))
                .ToListAsync();
        }
        public async Task<T?> GetByIdAsync(int id) => await _entities.FindAsync(id);
        public async Task AddAsync(T entity)
        {
            await _entities.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }

        public async Task DeleteByIdAsync(int id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity != null)
            {
                _context.Remove(entity);
            }
        }

        public async Task<List<T>> GetByDayAsync(DateTime time)
        {
            // 1. 剥离时分秒，仅保留年月日（得到当天00:00:00）
            DateTime startDate = time.Date;
            // 2. 构建时间范围：当天0点 到 次日0点（左闭右开，无需计算最后一刻）
            DateTime endDate = startDate.AddDays(1);

            // 3. 执行查询：筛选当天数据 + 正序排序

            return await _entities
                .Where(e =>
                    // 匹配当天所有时间（忽略createdtime的时分秒）
                    EF.Property<DateTime>(e, "createdtime") >= startDate
                    && EF.Property<DateTime>(e, "createdtime") < endDate)
                .OrderBy(e => EF.Property<DateTime>(e, "createdtime")) // 正序排序（默认ASC）
                .ToListAsync();
        }
    }
}
