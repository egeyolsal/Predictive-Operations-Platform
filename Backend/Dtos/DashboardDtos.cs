using System.ComponentModel.DataAnnotations;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Dtos;

public class DashboardDto
{
    // Admin specific
    public int? TotalActiveInventory { get; set; }
    public int? LowStockCount { get; set; }
    
    // Shared / Role Specific
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    
    // Trends (Simplified)
    public double PendingTasksTrend { get; set; }
    public double InProgressTasksTrend { get; set; }
    public double CompletedTasksTrend { get; set; }
    
    // Chart Data
    public List<TaskActivityDto> TaskActivity { get; set; } = new();
    public List<TopInventoryUsedDto> TopInventoryUsed { get; set; } = new();
    public List<StaffPerformanceDto> StaffPerformance { get; set; } = new();
}

public class TaskActivityDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopInventoryUsedDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class StaffPerformanceDto
{
    public string StaffName { get; set; } = string.Empty;
    public int CompletedTasks { get; set; }
}
