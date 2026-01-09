


using System.Linq.Expressions;

namespace BuildingBlocks.Repository
{
    public interface IGenericRepository<T> where T : class
    {
        Task AddAsync(T entity);
        IQueryable<T> Entities { get; }
        Task UpdateAsync(T entity);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task DeleteAsyncById(object id);
        Task DeleteAsync(params object[] keyValues);
        Task RemoveRange(IEnumerable<T> entities);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<(List<T> Items, long TotalCount)> GetAllByPropertyWithCountAsync(int pageNumber = 1, int pageSize = 10, Expression<Func<T, bool>>? filter = null, string? includeProperties = null, Expression<Func<T, object>>? orderBy = null, bool ascending = true);
        Task<T> GetByPropertyAsync(Expression<Func<T, bool>>? filter = null, bool tracked = true, string? includeProperties = null);
        Task<List<T>> GetAllByPropertyAsync(Expression<Func<T, bool>>? filter = null, IEnumerable<Expression<Func<T, object>>>? includes = null, bool asTracking = false);

    }
}
