using Microsoft.EntityFrameworkCore;
using TaskInventoryApi.Models;
using TaskInventoryApi.Services;

namespace TaskInventoryApi.Data;

public static class DataSeeder
{
    private static readonly Random _random = new(42);
    private const string SeedMarker = "Seed data ile otomatik oluşturuldu.";

    public static async Task SeedAsync(ApplicationDbContext context, ITaskAnomalyService anomalyService)
    {
        // NOTE: Migrations must be applied BEFORE seeding. Run 'dotnet ef database update' first.
        // EnsureCreatedAsync() is intentionally NOT used here as it conflicts with EF Core migrations.
        if (await context.Users.AnyAsync())
        {
            Console.WriteLine("DB already contains data, skipping seed.");
            return;
        }

        var users = await SeedUsersAsync(context);
        var categories = await SeedCategoriesAsync(context);
        var suppliers = await SeedSuppliersAsync(context);
        var customers = await SeedCustomersAsync(context);
        var inventoryItems = await SeedInventoryItemsAsync(context, suppliers, categories);
        var tasks = await SeedTasksAsync(context, categories, users);
        await SeedInventoryTransactionsAsync(context, tasks, inventoryItems);
        await SeedInvoicesAsync(context, inventoryItems, customers);
        
        await RunAnomalyAnalysisOnSeededTasksAsync(context, anomalyService);

        Console.WriteLine("✅ Advanced Seed data created successfully.");
    }

    private static async Task<List<User>> SeedUsersAsync(ApplicationDbContext context)
    {
        var adminPassword = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        var workerPassword = BCrypt.Net.BCrypt.HashPassword("Worker123!");
        
        var admin = new User { Username = "admin_user", Email = "admin@worksight.com", PasswordHash = adminPassword, Role = UserRole.Admin };
        var worker1 = new User { Username = "ahmet_y", Email = "ahmet@worksight.com", PasswordHash = workerPassword, Role = UserRole.Worker };
        var worker2 = new User { Username = "mehmet_d", Email = "mehmet@worksight.com", PasswordHash = workerPassword, Role = UserRole.Worker };
        var analyst = new User { Username = "ayse_a", Email = "ayse@worksight.com", PasswordHash = workerPassword, Role = UserRole.Analyst };
        
        await context.Users.AddRangeAsync(admin, worker1, worker2, analyst);
        await context.SaveChangesAsync();

        return new List<User> { worker1, worker2 };
    }

    private static async Task<List<Category>> SeedCategoriesAsync(ApplicationDbContext context)
    {
        var cats = new List<Category>
        {
            new Category { Name = "Mekanik Bakım" },
            new Category { Name = "Elektrik Bakım" },
            new Category { Name = "Sarf Malzeme Değişimi" }
        };
        await context.Categories.AddRangeAsync(cats);
        await context.SaveChangesAsync();
        return cats;
    }

    private static async Task<List<Supplier>> SeedSuppliersAsync(ApplicationDbContext context)
    {
        var sups = new List<Supplier>
        {
            new Supplier { Name = "Demirhan Hırdavat", ContactName = "Ali Demir", Phone = "05551112233", Email = "ali@demirhan.com" },
            new Supplier { Name = "Volt Elektronik A.Ş.", ContactName = "Ayşe Yılmaz", Phone = "05554445566", Email = "ayse@volt.com" },
            new Supplier { Name = "Grup Endüstriyel", ContactName = "Hasan Kaya", Phone = "05557778899", Email = "hasan@grupend.com" }
        };
        await context.Suppliers.AddRangeAsync(sups);
        await context.SaveChangesAsync();
        return sups;
    }

    private static async Task<List<Customer>> SeedCustomersAsync(ApplicationDbContext context)
    {
        var custs = new List<Customer>
        {
            new Customer { Name = "Mega İnşaat Projesi", Email = "info@megainsaat.com", Phone = "02120000001", Address = "Şantiye 1" },
            new Customer { Name = "Oto-Parça Fabrikası", Email = "bakim@otofabrika.com", Phone = "02120000002", Address = "Fabrika Ana Bina" }
        };
        await context.Customers.AddRangeAsync(custs);
        await context.SaveChangesAsync();
        return custs;
    }

    private static async Task<List<InventoryItem>> SeedInventoryItemsAsync(ApplicationDbContext context, List<Supplier> suppliers, List<Category> categories)
    {
        var items = new List<InventoryItem>
        {
            new InventoryItem { Name = "Endüstriyel Rulman (SKF)", CategoryId = categories[0].Id, Barcode = "B001", CurrentStock = 120, CriticalThreshold = 20 },
            new InventoryItem { Name = "10mm Çelik Cıvata", CategoryId = categories[2].Id, Barcode = "B002", CurrentStock = 1500, CriticalThreshold = 200 },
            new InventoryItem { Name = "3x2.5 NYM Kablo (100m)", CategoryId = categories[1].Id, Barcode = "B003", CurrentStock = 40, CriticalThreshold = 10 },
            new InventoryItem { Name = "Motor Yağı 5W-30 (Varil)", CategoryId = categories[2].Id, Barcode = "B004", CurrentStock = 15, CriticalThreshold = 4 },
            new InventoryItem { Name = "30mA Kaçak Akım Rölesi", CategoryId = categories[1].Id, Barcode = "B005", CurrentStock = 30, CriticalThreshold = 5 },
        };
        await context.InventoryItems.AddRangeAsync(items);
        await context.SaveChangesAsync();

        // Tedarikçi-Ürün fiyat eşleştirmeleri (ItemSuppliers)
        var itemSuppliers = new List<ItemSupplier>
        {
            new ItemSupplier { InventoryItemId = items[0].Id, SupplierId = suppliers[0].Id, Price = 150.0m },
            new ItemSupplier { InventoryItemId = items[0].Id, SupplierId = suppliers[2].Id, Price = 145.0m }, // Daha ucuz
            new ItemSupplier { InventoryItemId = items[1].Id, SupplierId = suppliers[0].Id, Price = 5.0m },
            new ItemSupplier { InventoryItemId = items[2].Id, SupplierId = suppliers[1].Id, Price = 1200.0m },
            new ItemSupplier { InventoryItemId = items[3].Id, SupplierId = suppliers[2].Id, Price = 2500.0m },
            new ItemSupplier { InventoryItemId = items[4].Id, SupplierId = suppliers[1].Id, Price = 450.0m },
        };
        await context.ItemSuppliers.AddRangeAsync(itemSuppliers);
        await context.SaveChangesAsync();

        return items;
    }

    private static async Task<List<TaskItem>> SeedTasksAsync(
        ApplicationDbContext context, List<Category> categories, List<User> workers)
    {
        const int tasksPerCategory = 15;
        var tasks = new List<TaskItem>();

        foreach (var category in categories)
        {
            var baseDuration = 2.0 + _random.NextDouble() * 3.0;

            for (int i = 0; i < tasksPerCategory; i++)
            {
                bool isAnomalous = (i >= tasksPerCategory - 2); // Son 2 görev anormal (süresi abartılmış)
                double actualDuration = isAnomalous ? baseDuration * 4.0 : baseDuration;

                var createdAt = DateTime.UtcNow.AddDays(-_random.Next(5, 30));
                var completedAt = createdAt.AddHours(actualDuration);

                var task = new TaskItem
                {
                    Title = $"{category.Name} Operasyonu #{i + 1} {(isAnomalous ? "(Zorlu)" : "")}",
                    Description = SeedMarker + (isAnomalous ? " Ciddi sorunlar yaşandı." : ""),
                    Status = TaskItemStatus.Done,
                    AssignedUserId = workers[_random.Next(workers.Count)].Id,
                    CategoryId = category.Id,
                    CreatedAt = createdAt,
                    CompletedAt = completedAt,
                    ExpectedDurationHours = Math.Round(baseDuration, 1),
                    IsAnomalous = false // Will be updated by anomaly service
                };

                tasks.Add(task);
            }
        }
        await context.TaskItems.AddRangeAsync(tasks);
        await context.SaveChangesAsync();
        return tasks;
    }

    private static async Task SeedInventoryTransactionsAsync(
        ApplicationDbContext context, List<TaskItem> tasks, List<InventoryItem> items)
    {
        foreach (var task in tasks)
        {
            // Her task için 1-2 farklı malzeme kullanalım
            int numItems = _random.Next(1, 3);
            for(int j = 0; j < numItems; j++)
            {
                var item = items[_random.Next(items.Count)];
                // Anormal görev ise malzeme kullanımını da çok abartalım
                int qty = task.IsAnomalous ? _random.Next(20, 50) : _random.Next(1, 5);

                context.InventoryTransactions.Add(new InventoryTransaction
                {
                    InventoryItemId = item.Id,
                    TaskItemId = task.Id,
                    QuantityUsed = qty,
                    TransactionDate = task.CompletedAt ?? DateTime.UtcNow
                });
                
                item.CurrentStock = Math.Max(0, item.CurrentStock - qty); // Negatif stoka düşmesini engelle
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedInvoicesAsync(ApplicationDbContext context, List<InventoryItem> items, List<Customer> customers)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "IN-2026-0001",
            InvoiceDate = DateTime.UtcNow.AddDays(-5),
            Type = InvoiceType.Inbound,
            CustomerId = null, // Alım
            LineItems = new List<InvoiceLineItem>
            {
                new InvoiceLineItem { InventoryItemId = items[0].Id, Quantity = 50, UnitPrice = 145.0m },
                new InvoiceLineItem { InventoryItemId = items[1].Id, Quantity = 500, UnitPrice = 4.5m }
            }
        };

        var invoice2 = new Invoice
        {
            InvoiceNumber = "OUT-2026-0001",
            InvoiceDate = DateTime.UtcNow.AddDays(-2),
            Type = InvoiceType.Outbound,
            CustomerId = customers[0].Id, // Satış
            LineItems = new List<InvoiceLineItem>
            {
                new InvoiceLineItem { InventoryItemId = items[0].Id, Quantity = 10, UnitPrice = 200.0m }
            }
        };

        await context.Invoices.AddRangeAsync(invoice, invoice2);
        await context.SaveChangesAsync();
    }

    private static async Task RunAnomalyAnalysisOnSeededTasksAsync(ApplicationDbContext context, ITaskAnomalyService anomalyService)
    {
        Console.WriteLine("Running Anomaly Analysis on seeded tasks...");
        var doneTasks = await context.TaskItems
            .Where(t => t.Status == TaskItemStatus.Done && t.CompletedAt != null)
            .ToListAsync();

        int anomalyCount = 0;
        foreach (var task in doneTasks)
        {
            bool isAnomalous = await anomalyService.EvaluateTaskAnomalyAsync(task);
            if (isAnomalous)
            {
                task.IsAnomalous = true;
                context.TaskItems.Update(task);
                anomalyCount++;
            }
        }
        
        if (anomalyCount > 0)
        {
            await context.SaveChangesAsync();
        }
        Console.WriteLine($"✅ Anomaly Analysis complete. Found {anomalyCount} anomalous tasks out of {doneTasks.Count}.");
    }
}