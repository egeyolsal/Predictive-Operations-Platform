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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
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
    }
}