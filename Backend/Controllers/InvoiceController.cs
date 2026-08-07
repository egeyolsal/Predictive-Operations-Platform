using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceResponseDto>>> GetAll()
    {
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        // For a full implementation, we need eager loading.
        // Doing basic mapping for now.
        return Ok(invoices.Select(invoice => new InvoiceResponseDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Type = invoice.Type.ToString(),
            CustomerId = invoice.CustomerId,
            TotalAmount = invoice.TotalAmount
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<InvoiceResponseDto>> Create(InvoiceCreateDto dto)
    {
        // 1. Basic validation
        if (dto.Type == InvoiceType.Outbound && !dto.CustomerId.HasValue)
        {
            return BadRequest("Customer is required for Outbound invoices.");
        }

        if (dto.Type == InvoiceType.Inbound && !dto.SupplierId.HasValue)
        {
            return BadRequest("Supplier is required for Inbound invoices.");
        }

        if (dto.CustomerId.HasValue)
        {
            var customerExists = await _unitOfWork.Customers.GetByIdAsync(dto.CustomerId.Value);
            if (customerExists == null)
            {
                return BadRequest($"Customer with ID {dto.CustomerId.Value} not found.");
            }
        }

        if (dto.SupplierId.HasValue)
        {
            var supplierExists = await _unitOfWork.Suppliers.GetByIdAsync(dto.SupplierId.Value);
            if (supplierExists == null)
            {
                return BadRequest($"Supplier with ID {dto.SupplierId.Value} not found.");
            }
        }

        // 1.5. Merge identical products (Eğer aynı üründen 2 kere girildiyse miktarları birleştir)
        dto.LineItems = dto.LineItems
            .GroupBy(li => li.InventoryItemId)
            .Select(g => new InvoiceLineItemCreateDto
            {
                InventoryItemId = g.Key,
                Quantity = g.Sum(li => li.Quantity),
                UnitPrice = g.First().UnitPrice // Fiyat olarak ilk girileni baz al
            }).ToList();

        // 2. Fetch all inventory items needed for this invoice
        var itemIds = dto.LineItems.Select(li => li.InventoryItemId).Distinct().ToList();
        
        // We need to fetch items manually or use a find method. For simplicity, we fetch one by one or create a custom method in repo.
        // Since we are using GenericRepository, we can fetch all and filter, or fetch one by one. 
        // For production, a custom repository method would be better, but we will iterate here.
        var inventoryItems = new Dictionary<int, InventoryItem>();
        foreach (var id in itemIds)
        {
            var item = await _unitOfWork.InventoryItems.GetByIdAsync(id);
            if (item == null)
            {
                return BadRequest($"Inventory item with ID {id} not found.");
            }
            inventoryItems[id] = item;
        }

        // 3. Stock validation (for Outbound and Internal Consumption)
        if (dto.Type == InvoiceType.Outbound || dto.Type == InvoiceType.InternalConsumption)
        {
            foreach (var lineItem in dto.LineItems)
            {
                var inventoryItem = inventoryItems[lineItem.InventoryItemId];
                if (inventoryItem.CurrentStock < lineItem.Quantity)
                {
                    return BadRequest($"Insufficient stock for item '{inventoryItem.Name}'. Requested: {lineItem.Quantity}, Available: {inventoryItem.CurrentStock}");
                }
            }
        }

        // 4. Create Invoice entity
        var invoice = new Invoice
        {
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            Type = dto.Type,
            CustomerId = dto.Type == InvoiceType.Outbound ? dto.CustomerId : null,
            SupplierId = dto.Type == InvoiceType.Inbound ? dto.SupplierId : null,
            TotalAmount = dto.LineItems.Sum(li => li.Quantity * li.UnitPrice)
        };

        // 5. Create Line Items and Update Stock
        foreach (var lineItemDto in dto.LineItems)
        {
            var inventoryItem = inventoryItems[lineItemDto.InventoryItemId];

            // Stock update logic
            if (dto.Type == InvoiceType.Inbound)
            {
                inventoryItem.CurrentStock += lineItemDto.Quantity;
            }
            else // Outbound or InternalConsumption
            {
                inventoryItem.CurrentStock -= lineItemDto.Quantity;

                // Check Critical Threshold
                if (inventoryItem.CurrentStock < inventoryItem.CriticalThreshold)
                {
                    // Find an Admin user to assign the task to
                    var adminUsers = await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.Admin);
                    var adminUser = adminUsers.FirstOrDefault();
                    
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
                        Description = $"Fatura çıkışı sonrası ürün stoğu {inventoryItem.CurrentStock} adede düşmüştür (Kritik Eşik: {inventoryItem.CriticalThreshold}). Lütfen acilen satın alma işlemlerini başlatın.\n\n**{supplierInfo}**",
                        Status = TaskItemStatus.ToDo,
                        AssignedUserId = adminUser != null ? adminUser.Id : 1, // Fallback to 1 if no admin found
                        CategoryId = 1, // Fallback category
                        ExpectedDurationHours = 2,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.TaskItems.AddAsync(reorderTask);
                }
            }
            _unitOfWork.InventoryItems.Update(inventoryItem);

            var lineItem = new InvoiceLineItem
            {
                InventoryItemId = lineItemDto.InventoryItemId,
                Quantity = lineItemDto.Quantity,
                UnitPrice = lineItemDto.UnitPrice
                // TotalPrice is computed
            };

            invoice.LineItems.Add(lineItem);
        }

        // 6. Save everything in a single transaction (Unit of Work)
        await _unitOfWork.Invoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        // 7. Map to Response
        Customer? customer = null;
        if (invoice.CustomerId.HasValue)
        {
            customer = await _unitOfWork.Customers.GetByIdAsync(invoice.CustomerId.Value);
        }

        Supplier? invoiceSupplier = null;
        if (invoice.SupplierId.HasValue)
        {
            invoiceSupplier = await _unitOfWork.Suppliers.GetByIdAsync(invoice.SupplierId.Value);
        }

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, MapToResponseDto(invoice, customer, invoiceSupplier, inventoryItems));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceResponseDto>> GetById(int id)
    {
        // For a full implementation, we need eager loading (Include LineItems). 
        // Our GenericRepository doesn't have Includes by default unless implemented.
        // We will do a basic fetch or manual mapping for now.
        
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
        if (invoice == null)
            return NotFound();
            
        // Fetch line items
        var allLineItems = await _unitOfWork.InvoiceLineItems.GetAllAsync();
        var lineItems = allLineItems.Where(li => li.InvoiceId == id).ToList();
        invoice.LineItems = lineItems;

        Customer? customer = null;
        if (invoice.CustomerId.HasValue)
        {
            customer = await _unitOfWork.Customers.GetByIdAsync(invoice.CustomerId.Value);
        }
        
        Supplier? supplier = null;
        if (invoice.SupplierId.HasValue)
        {
            supplier = await _unitOfWork.Suppliers.GetByIdAsync(invoice.SupplierId.Value);
        }
        
        var allInventoryItems = await _unitOfWork.InventoryItems.GetAllAsync();
        var inventoryItemsDict = allInventoryItems.ToDictionary(i => i.Id);

        return Ok(MapToResponseDto(invoice, customer, supplier, inventoryItemsDict));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelInvoice(int id)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
        if (invoice == null)
            return NotFound($"Invoice with ID {id} not found.");

        if (invoice.IsCancelled)
            return BadRequest("Invoice is already cancelled.");

        // Fetch line items
        var allLineItems = await _unitOfWork.InvoiceLineItems.GetAllAsync();
        var lineItems = allLineItems.Where(li => li.InvoiceId == id).ToList();

        // Reverse the stock changes
        foreach (var lineItem in lineItems)
        {
            var inventoryItem = await _unitOfWork.InventoryItems.GetByIdAsync(lineItem.InventoryItemId);
            if (inventoryItem != null)
            {
                if (invoice.Type == InvoiceType.Inbound)
                {
                    // Inbound added stock, so we subtract
                    inventoryItem.CurrentStock -= lineItem.Quantity;
                    // Prevent negative stock just in case
                    if (inventoryItem.CurrentStock < 0) inventoryItem.CurrentStock = 0;
                }
                else // Outbound or InternalConsumption
                {
                    // Outbound subtracted stock, so we add it back
                    inventoryItem.CurrentStock += lineItem.Quantity;
                }
                _unitOfWork.InventoryItems.Update(inventoryItem);
            }
        }

        invoice.IsCancelled = true;
        _unitOfWork.Invoices.Update(invoice);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Invoice successfully cancelled and stock reverted." });
    }

    private static InvoiceResponseDto MapToResponseDto(Invoice invoice, Customer? customer, Supplier? supplier, Dictionary<int, InventoryItem> inventoryItems)
    {
        return new InvoiceResponseDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Type = invoice.Type.ToString(),
            CustomerId = invoice.CustomerId,
            CustomerName = customer?.Name,
            SupplierId = invoice.SupplierId,
            SupplierName = supplier?.Name,
            TotalAmount = invoice.TotalAmount,
            IsCancelled = invoice.IsCancelled,
            LineItems = invoice.LineItems.Select(li => new InvoiceLineItemResponseDto
            {
                Id = li.Id,
                InventoryItemId = li.InventoryItemId,
                InventoryItemName = inventoryItems.ContainsKey(li.InventoryItemId) ? inventoryItems[li.InventoryItemId].Name : "Unknown",
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TotalPrice = li.TotalPrice
            }).ToList()
        };
    }
}
