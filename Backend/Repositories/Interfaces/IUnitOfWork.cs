using TaskInventoryApi.Models;

namespace TaskInventoryApi.Repositories;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<TaskItem> TaskItems { get; }
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<InventoryItem> InventoryItems { get; }
    IGenericRepository<InventoryTransaction> InventoryTransactions { get; }
    IGenericRepository<Customer> Customers { get; }
    IGenericRepository<Invoice> Invoices { get; }
    IGenericRepository<InvoiceLineItem> InvoiceLineItems { get; }
    IGenericRepository<Supplier> Suppliers { get; }
    IGenericRepository<ItemSupplier> ItemSuppliers { get; }

    Task<int> SaveChangesAsync();
}