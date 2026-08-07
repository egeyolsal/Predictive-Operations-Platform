namespace TaskInventoryApi.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation property
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}