using HMS.Application.Interfaces.Repositories;

namespace HMS.Application.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task ExecuteInTransactionAsync(Func<Task> operation);
}
