

using System.Data;

namespace BuildingBlocks.Repository
{
    public interface IUnitOfWork
    {
        IGenericRepository<T> GetRepository<T>() where T : class;

        Task SaveAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        IDbConnection GetDbConnection();
        IDbConnection? GetCurrentdTransaction();

    }
}
