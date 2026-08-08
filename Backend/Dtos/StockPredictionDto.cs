namespace TaskInventoryApi.Dtos;

public class StockPredictionDto
{
    public int InventoryItemId { get; set; }
    public string InventoryItemName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int CriticalThreshold { get; set; }
    
    // Moving Average stats
    public double DailyConsumptionRate { get; set; }
    
    // Predictions
    // Nullable in case DailyConsumptionRate is 0 (Division by Zero protection)
    public double? DaysUntilZero { get; set; }
    public double? DaysUntilCritical { get; set; }
    
    public bool IsCritical { get; set; }
}
