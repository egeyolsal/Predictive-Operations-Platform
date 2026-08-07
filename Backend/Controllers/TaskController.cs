using System.Security.Claims;
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
        existingTask.Status = dto.Status;
        existingTask.Priority = dto.Priority;
        existingTask.AssignedUserId = dto.AssignedUserId;
        existingTask.CategoryId = dto.CategoryId;
        existingTask.ExpectedDurationHours = dto.ExpectedDurationHours;

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

        existingTask.Status = dto.Status;
        if (dto.Status == TaskItemStatus.Done && existingTask.CompletedAt == null)
            existingTask.CompletedAt = DateTime.UtcNow;

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

        // 5. Update Stock & Check Critical Threshold
        inventoryItem.CurrentStock -= dto.Quantity;
        _unitOfWork.InventoryItems.Update(inventoryItem);

        // --- PREDICTIVE ANALYTICS ALGORITHM ---
        // 5.1 Calculate Daily Consumption (Velocity) over the last 30 days
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var pastTransactions = await _unitOfWork.InventoryTransactions.FindAsync(
            it => it.InventoryItemId == inventoryItem.Id && it.TransactionDate >= thirtyDaysAgo);
        
        int totalConsumedIn30Days = pastTransactions.Sum(it => it.QuantityUsed) + dto.Quantity;
        double dailyConsumptionRate = totalConsumedIn30Days / 30.0;

        // 5.2 Calculate Days Until Stockout
        double daysUntilStockOut = 999;
        if (dailyConsumptionRate > 0)
        {
            daysUntilStockOut = inventoryItem.CurrentStock / dailyConsumptionRate;
        }

        // 5.3 Fetch Supplier Info & Lead Time (Already fetched above)
        int leadTimeDays = optimumItemSupplier?.LeadTimeDays ?? 3; // Default 3 days

        // 5.4 Decision: Is it critical? (Stock runs out before supplier can deliver, OR stock out in < 3 days)
        bool isCritical = (daysUntilStockOut <= leadTimeDays) || (daysUntilStockOut <= 3.0) || (inventoryItem.CurrentStock < inventoryItem.CriticalThreshold);

        if (isCritical && inventoryItem.CurrentStock > 0)
        {
            var adminUsers = await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.Admin);
            var adminUser = adminUsers.FirstOrDefault();
            int assignedUserId = adminUser?.Id ?? task.AssignedUserId;

            string supplierInfo = "Optimum tedarikçi bulunamadı.";
            if (optimumItemSupplier != null)
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(optimumItemSupplier.SupplierId);
                if (supplier != null)
                {
                    supplierInfo = $"Önerilen Tedarikçi: {supplier.Name} (Fiyat: {optimumItemSupplier.Price} TL)\nE-posta: {supplier.Email ?? "bulunamadi@tedarik.com"}\nLead Time: {optimumItemSupplier.LeadTimeDays} Gün";
                }
            }

            var reorderTask = new TaskItem
            {
                Title = $"🚨 Kritik Stok Uyarısı: {inventoryItem.Name}",
                Description = $"**🤖 YAPAY ZEKA STOK ÖNGÖRÜSÜ (PREDICTIVE ANALYTICS)**\n\n" +
                              $"- **Son 30 Gün Tüketim:** {totalConsumedIn30Days} adet\n" +
                              $"- **Günlük Tüketim Hızı (Velocity):** {Math.Round(dailyConsumptionRate, 2)} adet/gün\n" +
                              $"- **Tahmini Tükenme Süresi:** {Math.Round(daysUntilStockOut, 1)} gün kaldı!\n\n" +
                              $"Sistem, mevcut {inventoryItem.CurrentStock} adet stoğun operasyonları aksatacak kadar hızlı tükendiğini tespit etti. Lütfen aşağıdaki bilgileri kullanarak derhal sipariş geçiniz.\n\n" +
                              $"---\n**Tedarikçi Analizi:**\n{supplierInfo}",
                Status = TaskItemStatus.ToDo,
                Priority = TaskPriority.High,
                AssignedUserId = assignedUserId,
                CategoryId = task.CategoryId,
                ExpectedDurationHours = 1,
                IsAnomalous = true,
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
        Priority = task.Priority,
        AssignedUserId = task.AssignedUserId,
        AssignedUserName = task.AssignedUser?.Username,
        CategoryId = task.CategoryId,
        CategoryName = task.Category?.Name,
        CreatedAt = task.CreatedAt,
        CompletedAt = task.CompletedAt,
        ExpectedDurationHours = task.ExpectedDurationHours,
        IsAnomalous = task.IsAnomalous
    };
}