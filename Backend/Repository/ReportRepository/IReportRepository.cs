using Microsoft.EntityFrameworkCore;

namespace CenterReport.Repository
{
    public interface IReportRepository<T> where T : class
    {
        IQueryable<T> db { get; }
        Task<List<T>> GetByDataTimeAsync(DateTime datetime, int _Type);
        Task<List<T>> GetByDataTimeAsync(DateTime start, DateTime end, int _Type);
        Task<List<T>> GetByDataTimeAsync(DateTime start, DateTime end);
        Task<List<T>> GetByDayAsync(DateTime time);
        Task<T?> GetByIdAsync(int id);
        Task AddAsync(T entity);
        void Update(T entity);
        Task DeleteByIdAsync(int id);
    }
}
