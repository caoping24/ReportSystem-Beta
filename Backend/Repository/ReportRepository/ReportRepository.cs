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

        public async Task<List<T>> GetByDataTimeAsync(DateTime dateTime, int Type)
        {
            return await GetByDataTimeAsync(dateTime, dateTime, Type);
        }
        public async Task<List<T>> GetByDataTimeAsync(DateTime start, DateTime end, int _Type)
        {
            var from = DateTime.Compare(start, end) > 0 ? end : start;
            var to = DateTime.Compare(start, end) > 0 ? start : end;

            if (from == to)//获取满足条件的最新的一条记录
            {
                return await _entities
                    .Where(e => EF.Property<DateTime>(e, "createdtime") == from && EF.Property<int>(e, "Type") == _Type)
                    .ToListAsync();
            }
            return await _entities
                .Where(e => EF.Property<DateTime>(e, "createdtime") >= from && EF.Property<DateTime>(e, "createdtime") <= to
                    && EF.Property<int>(e, "Type") == _Type)
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


        /// <summary>
        /// 查询一天内的数据（昨日08:00-今日20:00）
        /// </summary>
        /// <param name="time">日期时间,自动切片</param>
        /// <returns></returns>
        public async Task<List<T>> GetByDayAsync(DateTime time)
        {

            DateTime startDate = time.Date.AddDays(-1).AddHours(8);//昨日08:00
            DateTime endDate = time.Date.AddMonths(20);//今日20:00

            // 3. 执行查询：筛选当天数据 + 正序排序

            return await _entities
                .Where(e =>
                    EF.Property<DateTime>(e, "createdtime") >= startDate// 匹配当天所有时间（忽略createdtime的时分秒）
                    && EF.Property<DateTime>(e, "createdtime") < endDate)
                .OrderBy(e => EF.Property<DateTime>(e, "createdtime")) // 正序排序（默认ASC）
                .ToListAsync();
        }
    }
}
