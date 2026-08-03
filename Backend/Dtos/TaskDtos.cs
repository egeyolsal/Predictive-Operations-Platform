using TaskInventoryApi.Models;

namespace TaskInventoryApi.Dtos;

public class TaskCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;
    public int AssignedUserId { get; set; }
    public int CategoryId { get; set; }
    public double ExpectedDurationHours { get; set; }
}

public class TaskUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public int AssignedUserId { get; set; }
    public int CategoryId { get; set; }
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