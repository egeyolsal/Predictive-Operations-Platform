using TaskInventoryApi.Models;

namespace TaskInventoryApi.Repositories;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<TaskItem> TaskItems { get; }
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<InventoryItem> InventoryItems { get; }
    IGenericRepository<InventoryTransaction> InventoryTransactions { get; }

    Task<int> SaveChangesAsync();
}