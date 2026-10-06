using System.Collections.Concurrent;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Repositories;
using HMS.Infrastructure.Persistence;

namespace HMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly HmsDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public UnitOfWork(HmsDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>() where T : class =>
        (IGenericRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new GenericRepository<T>(_context));

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await operation();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
