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
    }
}
