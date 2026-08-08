using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Services;

public class StockPredictionService : IStockPredictionService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockPredictionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StockPredictionDto> CalculatePredictionAsync(InventoryItem item)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        
        // Sadece son 30 günün hareketlerini getir
        var pastTransactions = await _unitOfWork.InventoryTransactions.FindAsync(
            it => it.InventoryItemId == item.Id && it.TransactionDate >= thirtyDaysAgo);
        
        int totalConsumedIn30Days = pastTransactions.Sum(it => it.QuantityUsed);
        double dailyConsumptionRate = totalConsumedIn30Days / 30.0;

        var dto = new StockPredictionDto
        {
            InventoryItemId = item.Id,
            InventoryItemName = item.Name,
            CurrentStock = item.CurrentStock,
            CriticalThreshold = item.CriticalThreshold,
            DailyConsumptionRate = dailyConsumptionRate
        };

        // DIVISION BY ZERO KORUMASI (User Feedback)
        if (dailyConsumptionRate > 0)
        {
            dto.DaysUntilZero = item.CurrentStock / dailyConsumptionRate;
            
            double remainingUntilCritical = item.CurrentStock - item.CriticalThreshold;
            if (remainingUntilCritical <= 0)
            {
                dto.DaysUntilCritical = 0;
            }
            else
            {
                dto.DaysUntilCritical = remainingUntilCritical / dailyConsumptionRate;
            }
        }
        else
        {
            // Hiç tüketim yoksa tükenme süresi belirsizdir (Sonsuz)
            dto.DaysUntilZero = null;
            dto.DaysUntilCritical = null;
        }

        // Tedarikçi lead time bilgisini alalım (Critical kararı için)
        var itemSuppliers = await _unitOfWork.ItemSuppliers.FindAsync(isup => isup.InventoryItemId == item.Id);
        var optimumItemSupplier = itemSuppliers.OrderBy(isup => isup.Price).FirstOrDefault();
        int leadTimeDays = optimumItemSupplier?.LeadTimeDays ?? 3; // Default 3 days

        // Kritik durum kararı:
        // Stok threshold'un altındaysa, VEYA bitmesine Lead Time'dan az kaldıysa VEYA 3 günden az kaldıysa.
        if (dto.DaysUntilZero.HasValue)
        {
            dto.IsCritical = (dto.DaysUntilZero.Value <= leadTimeDays) || (dto.DaysUntilZero.Value <= 3.0) || (item.CurrentStock < item.CriticalThreshold);
        }
        else
        {
            dto.IsCritical = (item.CurrentStock < item.CriticalThreshold);
        }

        return dto;
    }

    public async Task<List<StockPredictionDto>> CalculateAllPredictionsAsync()
    {
        var items = await _unitOfWork.InventoryItems.GetAllAsync();
        var predictions = new List<StockPredictionDto>();
        
        foreach (var item in items)
        {
            predictions.Add(await CalculatePredictionAsync(item));
        }
        
        return predictions.OrderBy(p => p.DaysUntilZero ?? double.MaxValue).ToList();
    }

    public async Task EvaluateStockAndCreateAlertsAsync(InventoryItem item, int triggerTaskId, int assignedUserId)
    {
        var prediction = await CalculatePredictionAsync(item);
        
        if (prediction.IsCritical && item.CurrentStock > 0)
        {
            var adminUsers = await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.Admin);
            var adminUser = adminUsers.FirstOrDefault();
            int alertAssignedUserId = adminUser?.Id ?? assignedUserId;

            var itemSuppliers = await _unitOfWork.ItemSuppliers.FindAsync(isup => isup.InventoryItemId == item.Id);
            var optimumItemSupplier = itemSuppliers.OrderBy(isup => isup.Price).FirstOrDefault();

            string supplierInfo = "No optimum supplier found.";
            if (optimumItemSupplier != null)
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(optimumItemSupplier.SupplierId);
                if (supplier != null)
                {
                    supplierInfo = $"Recommended Supplier: {supplier.Name} (Price: ${optimumItemSupplier.Price})\nEmail: {supplier.Email ?? "notfound@supplier.com"}\nLead Time: {optimumItemSupplier.LeadTimeDays} Days";
                }
            }
            
            string daysZeroText = prediction.DaysUntilZero.HasValue ? Math.Round(prediction.DaysUntilZero.Value, 1).ToString() : "Unknown";

            var reorderTask = new TaskItem
            {
                Title = $"🚨 Critical Stock Alert: {item.Name}",
                Description = $"**🛡️ CRITICAL STOCK FORECAST & PROTECTION**\n\n" +
                              $"- **Daily Consumption (Velocity):** {Math.Round(prediction.DailyConsumptionRate, 2)} units/day\n" +
                              $"- **Estimated Days Until Empty:** {daysZeroText} days left!\n\n" +
                              $"The system has detected that the current stock of {item.CurrentStock} units is depleting fast enough to disrupt operations. Please place an order immediately using the information below.\n\n" +
                              $"---\n**Supplier Analysis:**\n{supplierInfo}",
                Status = TaskItemStatus.ToDo,
                Priority = TaskPriority.High,
                AssignedUserId = alertAssignedUserId,
                CategoryId = 1, // Fallback CategoryId if task.CategoryId is not passed properly
                ExpectedDurationHours = 1,
                IsAnomalous = true,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TaskItems.AddAsync(reorderTask);
            await _unitOfWork.SaveChangesAsync(); // We save immediately to ensure the task is created
        }
    }
}
