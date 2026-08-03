using TaskInventoryApi.Data;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    private IGenericRepository<User>? _users;
    private IGenericRepository<TaskItem>? _taskItems;
    private IGenericRepository<Category>? _categories;
    private IGenericRepository<InventoryItem>? _inventoryItems;
    private IGenericRepository<InventoryTransaction>? _inventoryTransactions;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<User> Users =>
        _users ??= new GenericRepository<User>(_context);

    public IGenericRepository<TaskItem> TaskItems =>
        _taskItems ??= new GenericRepository<TaskItem>(_context);

    public IGenericRepository<Category> Categories =>
        _categories ??= new GenericRepository<Category>(_context);

    public IGenericRepository<InventoryItem> InventoryItems =>
        _inventoryItems ??= new GenericRepository<InventoryItem>(_context);

    public IGenericRepository<InventoryTransaction> InventoryTransactions =>
        _inventoryTransactions ??= new GenericRepository<InventoryTransaction>(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}