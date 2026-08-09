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
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications()
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirst("role")?.Value;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value ?? User.FindFirst("sub")?.Value;
        
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        var notifications = new List<NotificationDto>();
        var now = DateTime.UtcNow;

        // Fetch tasks assigned to the current user (ANY role) that are in ToDo state
        var myTasks = await _context.TaskItems
            .AsNoTracking()
            .Where(t => t.AssignedUserId == userId && t.Status == TaskItemStatus.ToDo)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .ToListAsync();

        foreach (var t in myTasks)
        {
            notifications.Add(new NotificationDto
            {
                Id = $"task_new_{t.Id}",
                Type = "task",
                Message = $"New task assigned: {t.Title}",
                Link = "/tasks",
                Date = t.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss'Z'")
            });
        }

        if (userRole == UserRole.Admin.ToString())
        {
            // Low stock alerts
            var lowStockItems = await _context.InventoryItems
                .AsNoTracking()
                .Include(i => i.Transactions)
                .Where(i => i.CurrentStock < i.CriticalThreshold)
                .OrderBy(i => i.CurrentStock)
                .Take(5)
                .ToListAsync();

            foreach (var i in lowStockItems)
            {
                var lastTransactionDate = i.Transactions.OrderByDescending(t => t.TransactionDate).FirstOrDefault()?.TransactionDate ?? now;
                notifications.Add(new NotificationDto
                {
                    Id = $"inv_low_{i.Id}",
                    Type = "inventory",
                    Message = $"Low stock alert: {i.Name} ({i.CurrentStock}/{i.CriticalThreshold})",
                    Link = "/inventory",
                    Date = lastTransactionDate.ToString("yyyy-MM-ddTHH:mm:ss'Z'")
                });
            }

            // Recently completed tasks (last 24h)
            var recentCompletedTasks = await _context.TaskItems
                .AsNoTracking()
                .Include(t => t.AssignedUser)
                .Where(t => t.Status == TaskItemStatus.Done && t.CompletedAt >= now.AddDays(-1))
                .OrderByDescending(t => t.CompletedAt)
                .Take(3)
                .ToListAsync();

            foreach (var t in recentCompletedTasks)
            {
                notifications.Add(new NotificationDto
                {
                    Id = $"task_done_{t.Id}",
                    Type = "task",
                    Message = $"Task completed by {t.AssignedUser?.Username}: {t.Title}",
                    Link = "/tasks",
                    Date = (t.CompletedAt ?? now).ToString("yyyy-MM-ddTHH:mm:ss'Z'")
                });
            }
            
            // Unassigned ToDo Tasks (Warning CS0472 fixed by ignoring this query or changing to logic)
            // AssignedUserId is 'int' and cannot be null. Since we don't allow unassigned tasks currently,
            // we can remove this block or check for ID = 0 if that's the default.
            // Let's remove this block since AssignedUserId is required.
        }

        // Sort all aggregated notifications by date descending
        return Ok(notifications.OrderByDescending(n => n.Date).ToList());
    }
}
