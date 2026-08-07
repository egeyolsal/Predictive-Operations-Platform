using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class SupplierController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierResponseDto>>> GetAll()
    {
        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        return Ok(suppliers.Select(s => new SupplierResponseDto
        {
            Id = s.Id,
            Name = s.Name,
            ContactName = s.ContactName,
            Phone = s.Phone,
            Email = s.Email
        }));
    }

    [HttpGet("{id}/items")]
    public async Task<ActionResult<IEnumerable<SupplierItemResponseDto>>> GetSupplierItems(int id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier == null) return NotFound("Supplier not found.");

        var itemSuppliers = await _unitOfWork.ItemSuppliers.FindAsync(
            isup => isup.SupplierId == id, 
            isup => isup.InventoryItem!);

        return Ok(itemSuppliers.Select(isup => new SupplierItemResponseDto
        {
            InventoryItemId = isup.InventoryItemId,
            InventoryItemName = isup.InventoryItem?.Name ?? "Unknown",
            Category = isup.InventoryItem?.Category ?? "Unknown",
            CurrentStock = isup.InventoryItem?.CurrentStock ?? 0,
            Price = isup.Price,
            LeadTimeDays = isup.LeadTimeDays
        }));
    }

    [HttpPost]
    public async Task<ActionResult<SupplierResponseDto>> Create(SupplierCreateDto dto)
    {
        var supplier = new Supplier
        {
            Name = dto.Name,
            ContactName = dto.ContactName,
            Phone = dto.Phone,
            Email = dto.Email
        };

        await _unitOfWork.Suppliers.AddAsync(supplier);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new SupplierResponseDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email
        });
    }

    [HttpPost("{id}/assign-item")]
    public async Task<IActionResult> AssignItem(int id, ItemSupplierAssignDto dto)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier == null) return NotFound("Supplier not found.");

        var item = await _unitOfWork.InventoryItems.GetByIdAsync(dto.InventoryItemId);
        if (item == null) return NotFound("Inventory item not found.");

        // Check if assignment already exists
        var existingAssignments = await _unitOfWork.ItemSuppliers.FindAsync(isup => isup.SupplierId == id && isup.InventoryItemId == dto.InventoryItemId);
        var existing = existingAssignments.FirstOrDefault();

        if (existing != null)
        {
            existing.Price = dto.Price;
            existing.LeadTimeDays = dto.LeadTimeDays;
            _unitOfWork.ItemSuppliers.Update(existing);
        }
        else
        {
            var assignment = new ItemSupplier
            {
                SupplierId = id,
                InventoryItemId = dto.InventoryItemId,
                Price = dto.Price,
                LeadTimeDays = dto.LeadTimeDays
            };
            await _unitOfWork.ItemSuppliers.AddAsync(assignment);
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, SupplierUpdateDto dto)
    {
        var existingSupplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (existingSupplier == null)
            return NotFound();

        existingSupplier.Name = dto.Name;
        existingSupplier.ContactName = dto.ContactName;
        existingSupplier.Phone = dto.Phone;
        existingSupplier.Email = dto.Email;

        _unitOfWork.Suppliers.Update(existingSupplier);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier == null)
            return NotFound();

        _unitOfWork.Suppliers.Remove(supplier);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}
