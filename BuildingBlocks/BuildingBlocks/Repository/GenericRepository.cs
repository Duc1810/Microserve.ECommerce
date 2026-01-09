using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
namespace BuildingBlocks.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly DbContext _context;
        private readonly DbSet<T> _dbSet;
        public GenericRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public IQueryable<T> Entities => _context.Set<T>();
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
        }
        public async Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }
        public async Task DeleteAsyncById(object id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }
        public async Task RemoveRange(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            _dbSet.RemoveRange(entities);
        }
        public async Task DeleteAsync(params object[] keyValues)
        {
            var entity = await _dbSet.FindAsync(keyValues);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public async Task<(List<T> Items, long TotalCount)> GetAllByPropertyWithCountAsync(int pageNumber = 1, int pageSize = 10, Expression<Func<T, bool>>? filter = null, string? includeProperties = null, Expression<Func<T, object>>? orderBy = null,
            bool ascending = true)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);


            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(includeProp.Trim());
            }

            var totalCount = await query.LongCountAsync();

            if (orderBy != null)
                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            else
                query = query.OrderBy(e => EF.Property<object>(e, "Id"));

            var skip = (pageNumber - 1) * pageSize;
            query = query.Skip(skip).Take(pageSize);

            var items = await query.ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<T>> GetAllByPropertyAsync(Expression<Func<T, bool>>? filter = null, IEnumerable<Expression<Func<T, object>>>? includes = null, bool asTracking = false)
        {
            IQueryable<T> query = asTracking ? _dbSet : _dbSet.AsNoTracking();

            if (includes != null)
                foreach (var inc in includes)
                    query = query.Include(inc);

            if (filter != null)
                query = query.Where(filter);

            return await query.ToListAsync();
        }






        public async Task<T?> GetByPropertyAsync(Expression<Func<T, bool>>? filter = null, bool tracked = true, string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;
            if (!tracked)
            {
                query = query.AsNoTracking();
            }
            query = query.AsNoTracking();
            if (includeProperties != null)
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp.Trim());
                }
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.FirstOrDefaultAsync();
        }
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }


    }
}
