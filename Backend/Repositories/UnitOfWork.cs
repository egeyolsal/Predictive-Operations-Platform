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
    private IGenericRepository<Customer>? _customers;
    private IGenericRepository<Invoice>? _invoices;
    private IGenericRepository<InvoiceLineItem>? _invoiceLineItems;
    private IGenericRepository<Supplier>? _suppliers;
    private IGenericRepository<ItemSupplier>? _itemSuppliers;

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

    public IGenericRepository<Customer> Customers =>
        _customers ??= new GenericRepository<Customer>(_context);

    public IGenericRepository<Invoice> Invoices =>
        _invoices ??= new GenericRepository<Invoice>(_context);

    public IGenericRepository<InvoiceLineItem> InvoiceLineItems =>
        _invoiceLineItems ??= new GenericRepository<InvoiceLineItem>(_context);

    public IGenericRepository<Supplier> Suppliers => 
        _suppliers ??= new GenericRepository<Supplier>(_context);

    public IGenericRepository<ItemSupplier> ItemSuppliers => 
        _itemSuppliers ??= new GenericRepository<ItemSupplier>(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}