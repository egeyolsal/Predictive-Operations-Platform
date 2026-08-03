namespace TaskInventoryApi.Models;

public class InventoryItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int CurrentStock { get; set; }

    public int CriticalThreshold { get; set; }

    // Navigation property
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}