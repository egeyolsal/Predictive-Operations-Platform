using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;
using TaskInventoryApi.Services;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskAnomalyService _anomalyService;
    private readonly IStockPredictionService _stockPredictionService;

    public TaskController(IUnitOfWork unitOfWork, ITaskAnomalyService anomalyService, IStockPredictionService stockPredictionService)
    {
        _unitOfWork = unitOfWork;
        _anomalyService = anomalyService;
        _stockPredictionService = stockPredictionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAll()
    {
        var tasks = await _unitOfWork.TaskItems.GetAllAsync(t => t.AssignedUser, t => t.Category);

        // Rol ve Kullanıcı ID'sini Token'dan al
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirst("role")?.Value;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value ?? User.FindFirst("sub")?.Value;

        // Eğer rol Worker ise sadece kendisine atananları döndür
        if (userRole == UserRole.Worker.ToString() && int.TryParse(userIdString, out int userId))
        {
            tasks = tasks.Where(t => t.AssignedUserId == userId);
        }

        return Ok(tasks.Select(MapToResponseDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponseDto>> GetById(int id)
    {
        var tasks = await _unitOfWork.TaskItems.FindAsync(t => t.Id == id, t => t.AssignedUser, t => t.Category);
        var task = tasks.FirstOrDefault();
        if (task == null)
            return NotFound();

        return Ok(MapToResponseDto(task));
    }

    [HttpGet("{id}/materials")]
    public async Task<ActionResult<IEnumerable<TaskMaterialResponseDto>>> GetMaterials(int id)
    {
        var transactions = await _unitOfWork.InventoryTransactions.FindAsync(
            it => it.TaskItemId == id,
            it => it.InventoryItem!);

        return Ok(transactions.Select(it => new TaskMaterialResponseDto
        {
            Id = it.Id,
            InventoryItemId = it.InventoryItemId,
            InventoryItemName = it.InventoryItem?.Name ?? "Unknown",
            QuantityUsed = it.QuantityUsed,
            TransactionDate = it.TransactionDate
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> Create(TaskCreateDto dto)
    {
        var categoryExists = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
        if (categoryExists == null)
            return BadRequest($"Category with id {dto.CategoryId} does not exist.");

        var userExists = await _unitOfWork.Users.GetByIdAsync(dto.AssignedUserId);
        if (userExists == null)
            return BadRequest($"User with id {dto.AssignedUserId} does not exist.");

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            Priority = dto.Priority,
            AssignedUserId = dto.AssignedUserId,
            CategoryId = dto.CategoryId,
            ExpectedDurationHours = dto.ExpectedDurationHours,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TaskItems.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, MapToResponseDto(task));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TaskUpdateDto dto)
    {
        var existingTask = await _unitOfWork.TaskItems.GetByIdAsync(id);
        if (existingTask == null)
            return NotFound();

        var categoryExists = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
        if (categoryExists == null)
            return BadRequest($"Category with id {dto.CategoryId} does not exist.");

        var userExists = await _unitOfWork.Users.GetByIdAsync(dto.AssignedUserId);
        if (userExists == null)
            return BadRequest($"User with id {dto.AssignedUserId} does not exist.");

        existingTask.Title = dto.Title;
        existingTask.Description = dto.Description;
        existingTask.Priority = dto.Priority;
        existingTask.AssignedUserId = dto.AssignedUserId;
        existingTask.CategoryId = dto.CategoryId;
        existingTask.ExpectedDurationHours = dto.ExpectedDurationHours;

        if (existingTask.Status != TaskItemStatus.Done && dto.Status == TaskItemStatus.Done)
        {
            existingTask.Status = dto.Status;
            existingTask.CompletedAt ??= DateTime.UtcNow;
            existingTask.IsAnomalous = await _anomalyService.EvaluateTaskAnomalyAsync(existingTask);
        }
        else
        {
            existingTask.Status = dto.Status;
        }

        _unitOfWork.TaskItems.Update(existingTask);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusDto dto)
    {
        var existingTask = await _unitOfWork.TaskItems.GetByIdAsync(id);
        if (existingTask == null)
            return NotFound();

        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirst("role")?.Value;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value ?? User.FindFirst("sub")?.Value;

        if (userRole == UserRole.Worker.ToString() && int.TryParse(userIdString, out int userId))
        {
            if (existingTask.AssignedUserId != userId)
                return Forbid();
        }

        if (existingTask.Status != TaskItemStatus.Done && dto.Status == TaskItemStatus.Done)
        {
            existingTask.Status = dto.Status;
            existingTask.CompletedAt ??= DateTime.UtcNow;
            existingTask.IsAnomalous = existingTask.IsSystemGenerated 
                ? false 
                : await _anomalyService.EvaluateTaskAnomalyAsync(existingTask);
        }
        else
        {
            existingTask.Status = dto.Status;
        }

        _unitOfWork.TaskItems.Update(existingTask);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _unitOfWork.TaskItems.GetByIdAsync(id);
        if (task == null)
            return NotFound();

        _unitOfWork.TaskItems.Remove(task);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("consume-material")]
    public async Task<IActionResult> ConsumeMaterial(TaskMaterialConsumptionDto dto)
    {
        // 1. Validate Task
        var task = await _unitOfWork.TaskItems.GetByIdAsync(dto.TaskId);
        if (task == null)
            return NotFound($"Task with ID {dto.TaskId} not found.");

        // 2. Find Inventory Item by Barcode
        var inventoryItems = await _unitOfWork.InventoryItems.FindAsync(i => i.Barcode == dto.Barcode);
        var inventoryItem = inventoryItems.FirstOrDefault();
        if (inventoryItem == null)
            return NotFound($"Product with barcode '{dto.Barcode}' not found.");

        // 3. Check Stock
        if (inventoryItem.CurrentStock < dto.Quantity)
            return BadRequest($"Insufficient stock for '{inventoryItem.Name}'. Requested: {dto.Quantity}, Available: {inventoryItem.CurrentStock}");

        // 3.5 Calculate Unit Cost based on Suppliers
        var itemSuppliers = await _unitOfWork.ItemSuppliers.FindAsync(isup => isup.InventoryItemId == inventoryItem.Id);
        var optimumItemSupplier = itemSuppliers.OrderBy(isup => isup.Price).FirstOrDefault();
        decimal unitCost = optimumItemSupplier?.Price ?? 0m;
        decimal totalCost = unitCost * dto.Quantity;

        // 4. Create Internal Consumption Invoice
        string shortCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        var invoice = new Invoice
        {
            InvoiceNumber = $"TASK-{task.Id}-{shortCode}",
            InvoiceDate = DateTime.UtcNow,
            Type = InvoiceType.InternalConsumption,
            TotalAmount = totalCost
        };

        var lineItem = new InvoiceLineItem
        {
            InventoryItemId = inventoryItem.Id,
            Quantity = dto.Quantity,
            UnitPrice = unitCost
        };
        invoice.LineItems.Add(lineItem);
        await _unitOfWork.Invoices.AddAsync(invoice);

        // 5. Update Stock
        inventoryItem.CurrentStock -= dto.Quantity;
        _unitOfWork.InventoryItems.Update(inventoryItem);

        // 6. Record Inventory Transaction
        var transaction = new InventoryTransaction
        {
            TaskItemId = task.Id,
            InventoryItemId = inventoryItem.Id,
            QuantityUsed = dto.Quantity,
            TransactionDate = DateTime.UtcNow
        };
        await _unitOfWork.InventoryTransactions.AddAsync(transaction);
        
        // Save these changes to the database BEFORE calculating the moving average
        // so the new transaction is included in the velocity calculation
        await _unitOfWork.SaveChangesAsync();

        // --- PREDICTIVE ANALYTICS ALGORITHM ---
        await _stockPredictionService.EvaluateStockAndCreateAlertsAsync(inventoryItem, task.Id, task.AssignedUserId);

        return Ok(new { Message = $"Consumed {dto.Quantity} of {inventoryItem.Name} for task {task.Id}.", InvoiceNumber = invoice.InvoiceNumber });
    }

    private static TaskResponseDto MapToResponseDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        AssignedUserId = task.AssignedUserId,
        AssignedUserName = task.AssignedUser?.Username,
        CategoryId = task.CategoryId,
        CategoryName = task.Category?.Name,
        CreatedAt = task.CreatedAt,
        CompletedAt = task.CompletedAt,
        ExpectedDurationHours = task.ExpectedDurationHours,
        IsAnomalous = task.IsAnomalous,
        IsSystemGenerated = task.IsSystemGenerated,
        RelatedInventoryItemId = task.RelatedInventoryItemId
    };
}