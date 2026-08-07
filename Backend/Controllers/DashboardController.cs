using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskInventoryApi.Data;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context; // For advanced LINQ grouping

    public DashboardController(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirst("role")?.Value;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value ?? User.FindFirst("sub")?.Value;
        
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        var dto = new DashboardDto();
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sevenDaysAgo = now.AddDays(-7);

        // Fetch tasks depending on role
        IQueryable<TaskItem> tasksQuery = _context.TaskItems.AsNoTracking();
        
        if (userRole == UserRole.Worker.ToString())
        {
            tasksQuery = tasksQuery.Where(t => t.AssignedUserId == userId);
        }
        else // Admin
        {
            var inventories = await _unitOfWork.InventoryItems.GetAllAsync();
            dto.TotalActiveInventory = inventories.Count();
            dto.LowStockCount = inventories.Count(i => i.CurrentStock < i.CriticalThreshold);
        }

        // General Task Stats (For both roles, filtered by query above)
        var tasksList = await tasksQuery.ToListAsync();

        dto.PendingTasks = tasksList.Count(t => t.Status == TaskItemStatus.ToDo);
        dto.InProgressTasks = tasksList.Count(t => t.Status == TaskItemStatus.InProgress);
        dto.CompletedTasks = tasksList.Count(t => t.Status == TaskItemStatus.Done);

        var sixtyDaysAgo = now.AddDays(-60);

        var currentCompleted = tasksList.Count(t => t.Status == TaskItemStatus.Done && t.CompletedAt >= thirtyDaysAgo);
        var prevCompleted = tasksList.Count(t => t.Status == TaskItemStatus.Done && t.CompletedAt >= sixtyDaysAgo && t.CompletedAt < thirtyDaysAgo);
        dto.CompletedTasksTrend = prevCompleted == 0 ? (currentCompleted > 0 ? 100 : 0) : Math.Round((double)(currentCompleted - prevCompleted) / prevCompleted * 100, 1);

        var currentInProgress = tasksList.Count(t => t.Status == TaskItemStatus.InProgress && t.CreatedAt >= thirtyDaysAgo);
        var prevInProgress = tasksList.Count(t => t.Status == TaskItemStatus.InProgress && t.CreatedAt >= sixtyDaysAgo && t.CreatedAt < thirtyDaysAgo);
        dto.InProgressTasksTrend = prevInProgress == 0 ? (currentInProgress > 0 ? 100 : 0) : Math.Round((double)(currentInProgress - prevInProgress) / prevInProgress * 100, 1);

        var currentPending = tasksList.Count(t => t.Status == TaskItemStatus.ToDo && t.CreatedAt >= thirtyDaysAgo);
        var prevPending = tasksList.Count(t => t.Status == TaskItemStatus.ToDo && t.CreatedAt >= sixtyDaysAgo && t.CreatedAt < thirtyDaysAgo);
        dto.PendingTasksTrend = prevPending == 0 ? (currentPending > 0 ? 100 : 0) : Math.Round((double)(currentPending - prevPending) / prevPending * 100, 1);

        // Chart Data - Task Activity (Last 7 Days)
        // Group by day of completion or creation
        var last7DaysTasks = tasksList.Where(t => t.CreatedAt >= sevenDaysAgo).ToList();
        
        for (int i = 6; i >= 0; i--)
        {
            var date = now.AddDays(-i).Date;
            dto.TaskActivity.Add(new TaskActivityDto
            {
                Date = date.ToString("MMM dd"),
                Count = last7DaysTasks.Count(t => t.CreatedAt.Date == date)
            });
        }

        // Only Admin gets advanced charts (Top Inventory, Staff Performance)
        if (userRole == UserRole.Admin.ToString())
        {
            // Top Inventory Used
            var topInventories = await _context.InventoryTransactions
                .Include(it => it.InventoryItem)
                .Where(it => it.TransactionDate >= sevenDaysAgo)
                .GroupBy(it => new { it.InventoryItemId, it.InventoryItem.Name })
                .Select(g => new TopInventoryUsedDto
                {
                    Name = g.Key.Name,
                    Quantity = g.Sum(it => it.QuantityUsed)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(3)
                .ToListAsync();

            dto.TopInventoryUsed = topInventories;

            // Staff Performance
            var staffPerformance = await _context.TaskItems
                .Include(t => t.AssignedUser)
                .Where(t => t.Status == TaskItemStatus.Done && t.CompletedAt >= sevenDaysAgo)
                .GroupBy(t => new { t.AssignedUserId, t.AssignedUser.Username })
                .Select(g => new StaffPerformanceDto
                {
                    StaffName = g.Key.Username,
                    CompletedTasks = g.Count()
                })
                .OrderByDescending(x => x.CompletedTasks)
                .Take(3)
                .ToListAsync();

            dto.StaffPerformance = staffPerformance;
        }

        return Ok(dto);
    }
}
