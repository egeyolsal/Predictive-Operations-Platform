using Microsoft.EntityFrameworkCore;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ItemSupplier> ItemSuppliers => Set<ItemSupplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(i => i.Name)
            .IsUnique();

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(i => i.Barcode)
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.AssignedUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(it => it.InventoryItem)
            .WithMany(i => i.Transactions)
            .HasForeignKey(it => it.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(it => it.TaskItem)
            .WithMany(t => t.InventoryTransactions)
            .HasForeignKey(it => it.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer -> Invoices
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Invoice -> InvoiceLineItems
        modelBuilder.Entity<InvoiceLineItem>()
            .HasOne(ili => ili.Invoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(ili => ili.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade); // If an invoice is deleted, its line items should be deleted

        // InventoryItem -> InvoiceLineItems
        modelBuilder.Entity<InvoiceLineItem>()
            .HasOne(ili => ili.InventoryItem)
            .WithMany(ii => ii.InvoiceLineItems)
            .HasForeignKey(ili => ili.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique Constraint: Bir faturada aynı üründen sadece 1 satır olabilir
        modelBuilder.Entity<InvoiceLineItem>()
            .HasIndex(ili => new { ili.InvoiceId, ili.InventoryItemId })
            .IsUnique();

        // ItemSupplier (Many-to-Many Composite Key)
        modelBuilder.Entity<ItemSupplier>()
            .HasKey(isup => new { isup.InventoryItemId, isup.SupplierId });

        modelBuilder.Entity<ItemSupplier>()
            .HasOne(isup => isup.InventoryItem)
            .WithMany(i => i.ItemSuppliers)
            .HasForeignKey(isup => isup.InventoryItemId);

        modelBuilder.Entity<ItemSupplier>()
            .HasOne(isup => isup.Supplier)
            .WithMany(s => s.ItemSuppliers)
            .HasForeignKey(isup => isup.SupplierId);
    }
}