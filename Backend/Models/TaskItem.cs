using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace TaskInventoryApi.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemStatus
{
    ToDo,
    InProgress,
    Done
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskPriority
{
    Low,
    Medium,
    High
}

public class TaskItem
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public int AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public double ExpectedDurationHours { get; set; }

    public bool IsAnomalous { get; set; } = false;

    // Navigation property
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}