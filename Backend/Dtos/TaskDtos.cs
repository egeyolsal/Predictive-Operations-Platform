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
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double ExpectedDurationHours { get; set; }
    public bool IsAnomalous { get; set; }
}