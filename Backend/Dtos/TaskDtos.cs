using System.ComponentModel.DataAnnotations;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Dtos;

public class TaskCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;
    
    [Required]
    public int AssignedUserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Range(0.1, 1000)]
    public double ExpectedDurationHours { get; set; }
}

public class TaskUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }

    [Required]
    public int AssignedUserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Range(0.1, 1000)]
    public double ExpectedDurationHours { get; set; }
}

public class TaskResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public int AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double ExpectedDurationHours { get; set; }
    public bool IsAnomalous { get; set; }
}

public class TaskMaterialConsumptionDto
{
    [Required]
    public int TaskId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Barcode { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}

public class TaskMaterialResponseDto
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public string InventoryItemName { get; set; } = string.Empty;
    public int QuantityUsed { get; set; }
    public DateTime TransactionDate { get; set; }
}