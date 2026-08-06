using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public TaskController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAll()
    {
        var tasks = await _unitOfWork.TaskItems.GetAllAsync();
        return Ok(tasks.Select(MapToResponseDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponseDto>> GetById(int id)
    {
        var task = await _unitOfWork.TaskItems.GetByIdAsync(id);
        if (task == null)
            return NotFound();

        return Ok(MapToResponseDto(task));
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
        existingTask.Status = dto.Status;
        existingTask.AssignedUserId = dto.AssignedUserId;
        existingTask.CategoryId = dto.CategoryId;
        existingTask.ExpectedDurationHours = dto.ExpectedDurationHours;

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

        // 4. Create Internal Consumption Invoice
        string shortCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        var invoice = new Invoice
        {
            InvoiceNumber = $"TASK-{task.Id}-{shortCode}",
            InvoiceDate = DateTime.UtcNow,
            Type = InvoiceType.InternalConsumption,
            TotalAmount = 0 // Usually 0 for internal consumption, or could be cost value
        };

        var lineItem = new InvoiceLineItem
        {
            InventoryItemId = inventoryItem.Id,
            Quantity = dto.Quantity,
            UnitPrice = 0 // Cost is handled separately or 0 for now
        };
        invoice.LineItems.Add(lineItem);
        await _unitOfWork.Invoices.AddAsync(invoice);

        // 5. Update Stock & Check Critical Threshold
        inventoryItem.CurrentStock -= dto.Quantity;
        _unitOfWork.InventoryItems.Update(inventoryItem);

        if (inventoryItem.CurrentStock < inventoryItem.CriticalThreshold)
        {
            // Find an Admin user to assign the task to
            var adminUsers = await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.Admin);
            var adminUser = adminUsers.FirstOrDefault();
            int assignedUserId = adminUser?.Id ?? task.AssignedUserId;

            // Find the optimum supplier (Lowest Price)
            string supplierInfo = "Optimum tedarikçi bulunamadı (Ürüne atanmış tedarikçi yok).";
            
            var itemSuppliers = await _unitOfWork.ItemSuppliers.FindAsync(isup => isup.InventoryItemId == inventoryItem.Id);
            var optimumItemSupplier = itemSuppliers.OrderBy(isup => isup.Price).FirstOrDefault();

            if (optimumItemSupplier != null)
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(optimumItemSupplier.SupplierId);
                if (supplier != null)
                {
                    supplierInfo = $"Optimum Tedarikçi: {supplier.Name} (Fiyat: {optimumItemSupplier.Price} TL, İletişim: {supplier.ContactName ?? "-"}, Tel: {supplier.Phone ?? "-"})";
                }
            }

            var reorderTask = new TaskItem
            {
                Title = $"⚠️ ACİL: '{inventoryItem.Name}' için Stok Kritik Seviyede",
                Description = $"Ürün stoğu {inventoryItem.CurrentStock} adede düşmüştür (Kritik Eşik: {inventoryItem.CriticalThreshold}). Lütfen acilen tedarik/satın alma işlemlerini başlatın.\n\n**{supplierInfo}**",
                Status = TaskItemStatus.ToDo,
                AssignedUserId = assignedUserId,
                CategoryId = task.CategoryId, // Or another category
                ExpectedDurationHours = 2,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TaskItems.AddAsync(reorderTask);
        }

        // 6. Record Inventory Transaction (Optional but good for history)
        var transaction = new InventoryTransaction
        {
            TaskItemId = task.Id,
            InventoryItemId = inventoryItem.Id,
            QuantityUsed = dto.Quantity,
            TransactionDate = DateTime.UtcNow
        };
        await _unitOfWork.InventoryTransactions.AddAsync(transaction);

        // 7. Save Transaction
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Material consumed successfully.", InvoiceNumber = invoice.InvoiceNumber });
    }

    private static TaskResponseDto MapToResponseDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        AssignedUserId = task.AssignedUserId,
        CategoryId = task.CategoryId,
        CreatedAt = task.CreatedAt,
        CompletedAt = task.CompletedAt,
        ExpectedDurationHours = task.ExpectedDurationHours,
        IsAnomalous = task.IsAnomalous
    };
}