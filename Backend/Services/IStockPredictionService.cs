using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Services;

public interface IStockPredictionService
{
    Task<StockPredictionDto> CalculatePredictionAsync(InventoryItem item);
    Task<List<StockPredictionDto>> CalculateAllPredictionsAsync();
    Task EvaluateStockAndCreateAlertsAsync(InventoryItem item, int triggerTaskId, int assignedUserId);
}
