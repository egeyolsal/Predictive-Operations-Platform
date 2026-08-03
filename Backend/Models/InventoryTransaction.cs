namespace TaskInventoryApi.Models;

public class InventoryTransaction
{
    public int Id { get; set; }

    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public int QuantityUsed { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}