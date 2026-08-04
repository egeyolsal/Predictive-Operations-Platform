using Microsoft.EntityFrameworkCore;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Data;

public static class DataSeeder
{
    // Sabit seed: her çalıştırmada aynı rastgele veriyi üretir, sonuçlar tekrarlanabilir olur.
    private static readonly Random _random = new(42);

    private const string SeedMarker = "Seed data ile otomatik oluşturuldu.";

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var categories = await SeedCategoriesAsync(context);
        var users = await SeedUsersAsync(context);
        var inventoryItems = await SeedInventoryItemsAsync(context);
        var tasks = await SeedTasksAsync(context, categories, users);
        await SeedInventoryTransactionsAsync(context, tasks, inventoryItems);

        Console.WriteLine("Seed data created successfully.");
    }

    private static async Task<List<Category>> SeedCategoriesAsync(ApplicationDbContext context)
    {
        var categoryNames = new[] { "Bakım", "Üretim", "Lojistik", "Kalite Kontrol" };
        var existingNames = await context.Categories.Select(c => c.Name).ToListAsync();

        foreach (var name in categoryNames)
        {
            if (!existingNames.Contains(name))
                context.Categories.Add(new Category { Name = name });
        }

        await context.SaveChangesAsync();
        return await context.Categories.ToListAsync();
    }

    private static async Task<List<User>> SeedUsersAsync(ApplicationDbContext context)
    {
        var existingUsernames = await context.Users.Select(u => u.Username).ToListAsync();
        var workerNames = new[] { "worker1", "worker2", "worker3", "worker4", "worker5" };

        foreach (var username in workerNames)
        {
            if (!existingUsernames.Contains(username))
            {
                context.Users.Add(new User
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Worker123!"),
                    Role = UserRole.Worker
                });
            }
        }

        await context.SaveChangesAsync();
        return await context.Users.Where(u => u.Role == UserRole.Worker).ToListAsync();
    }

    private static async Task<List<InventoryItem>> SeedInventoryItemsAsync(ApplicationDbContext context)
    {
        var itemsToSeed = new (string Name, string Category, int Stock, int Threshold)[]
        {
            ("Hidrolik Yağ", "Bakım", 200, 30),
            ("Endüstriyel Vida Seti", "Bakım", 500, 50),
            ("Kaynak Teli", "Üretim", 300, 40),
            ("Filtre Seti", "Bakım", 150, 20),
            ("Ambalaj Kartonu", "Lojistik", 1000, 100)
        };

        var existingNames = await context.InventoryItems.Select(i => i.Name).ToListAsync();

        foreach (var item in itemsToSeed)
        {
            if (!existingNames.Contains(item.Name))
            {
                context.InventoryItems.Add(new InventoryItem
                {
                    Name = item.Name,
                    Category = item.Category,
                    CurrentStock = item.Stock,
                    CriticalThreshold = item.Threshold
                });
            }
        }

        await context.SaveChangesAsync();
        return await context.InventoryItems.ToListAsync();
    }

    private static async Task<List<TaskItem>> SeedTasksAsync(
        ApplicationDbContext context, List<Category> categories, List<User> users)
    {
        const int tasksPerCategory = 9; // 4 kategori x 9 = 36 görev

        var existingSeeded = await context.TaskItems
            .Where(t => t.Description == SeedMarker)
            .ToListAsync();

        if (existingSeeded.Count >= tasksPerCategory * categories.Count)
        {
            Console.WriteLine("Tasks already seeded, skipping.");
            return existingSeeded;
        }

        var tasks = new List<TaskItem>();

        foreach (var category in categories)
        {
            // Her kategorinin kendine özgü bir ortalama süresi var (2-6 saat arası)
            var baseDuration = 2.0 + _random.NextDouble() * 4.0;

            for (int i = 0; i < tasksPerCategory; i++)
            {
                // Her kategorideki son görevi bilerek aykırı değer yapıyoruz
                bool isDeliberateOutlier = i == tasksPerCategory - 1;

                double actualDuration = isDeliberateOutlier
                    ? baseDuration * 3.5 // ortalamanın çok üzerinde - anomali tespitini tetiklemek için
                    : Math.Max(0.5, NextGaussian(baseDuration, baseDuration * 0.2));

                var createdAt = DateTime.UtcNow.AddDays(-_random.Next(5, 45));
                var completedAt = createdAt.AddHours(actualDuration);

                var task = new TaskItem
                {
                    Title = $"{category.Name} görevi #{i + 1}",
                    Description = SeedMarker,
                    Status = TaskItemStatus.Done,
                    AssignedUserId = users[_random.Next(users.Count)].Id,
                    CategoryId = category.Id,
                    CreatedAt = createdAt,
                    CompletedAt = completedAt,
                    ExpectedDurationHours = Math.Round(baseDuration, 1),
                    IsAnomalous = false // Gün 13'teki algoritma dolduracak, şimdiden işaretlemiyoruz
                };

                tasks.Add(task);
                context.TaskItems.Add(task);
            }
        }

        await context.SaveChangesAsync();
        return tasks;
    }

    private static async Task SeedInventoryTransactionsAsync(
        ApplicationDbContext context, List<TaskItem> tasks, List<InventoryItem> items)
    {
        const int transactionCount = 60;

        if (await context.InventoryTransactions.CountAsync() >= transactionCount)
        {
            Console.WriteLine("Inventory transactions already seeded, skipping.");
            return;
        }

        for (int i = 0; i < transactionCount; i++)
        {
            var item = items[_random.Next(items.Count)];
            var task = tasks[_random.Next(tasks.Count)];

            context.InventoryTransactions.Add(new InventoryTransaction
            {
                InventoryItemId = item.Id,
                TaskItemId = task.Id,
                QuantityUsed = _random.Next(1, 10),
                TransactionDate = DateTime.UtcNow.AddDays(-_random.Next(0, 20)) // son 20 gün içinde dağıtılmış
            });
        }

        await context.SaveChangesAsync();
    }

    // Box-Muller dönüşümü: normal (gauss) dağılıma yakın rastgele sayı üretir
    private static double NextGaussian(double mean, double stdDev)
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}