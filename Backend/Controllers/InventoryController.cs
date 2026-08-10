using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryResponseDto>>> GetAll()
    {
        var items = await _unitOfWork.InventoryItems.FindAsync(i => true, i => i.Category!);
        return Ok(items.Select(MapToResponseDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryResponseDto>> GetById(int id)
    {
        var items = await _unitOfWork.InventoryItems.FindAsync(i => i.Id == id, i => i.Category!);
        var item = items.FirstOrDefault();
        if (item == null)
            return NotFound();

        return Ok(MapToResponseDto(item));
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<InventoryResponseDto>> GetByBarcode(string barcode)
    {
        var items = await _unitOfWork.InventoryItems.FindAsync(i => i.Barcode == barcode, i => i.Category!);
        var item = items.FirstOrDefault();
        
        if (item == null)
            return NotFound();

        return Ok(MapToResponseDto(item));
    }

    [HttpGet("{id}/suppliers")]
    public async Task<ActionResult<IEnumerable<ItemSupplierResponseDto>>> GetSuppliers(int id)
    {
        var itemSuppliers = await _unitOfWork.ItemSuppliers.FindAsync(
            isup => isup.InventoryItemId == id,
            isup => isup.Supplier!);
            
        var dtos = itemSuppliers.Select(isup => new ItemSupplierResponseDto
        {
            SupplierId = isup.SupplierId,
            SupplierName = isup.Supplier?.Name ?? "Unknown",
            Price = isup.Price,
            LeadTimeDays = isup.LeadTimeDays
        });

        return Ok(dtos);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<InventoryResponseDto>> Create(InventoryCreateDto dto)
    {
        var categoryExists = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
        if (categoryExists == null)
            return BadRequest($"Category with id {dto.CategoryId} does not exist.");

        var duplicateName = await _unitOfWork.InventoryItems.FindAsync(i => i.Name.ToLower() == dto.Name.ToLower());
        if (duplicateName.Any())
            return BadRequest(new { message = $"An inventory item with the name '{dto.Name}' already exists." });
            
        var duplicateBarcode = await _unitOfWork.InventoryItems.FindAsync(i => i.Barcode == dto.Barcode);
        if (duplicateBarcode.Any())
            return BadRequest(new { message = $"An inventory item with the barcode '{dto.Barcode}' already exists." });

        var item = new InventoryItem
        {
            Name = dto.Name,
            CategoryId = dto.CategoryId,
            Barcode = dto.Barcode,
            CurrentStock = dto.CurrentStock,
            CriticalThreshold = dto.CriticalThreshold
        };

        await _unitOfWork.InventoryItems.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, MapToResponseDto(item));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, InventoryUpdateDto dto)
    {
        var existingItem = await _unitOfWork.InventoryItems.GetByIdAsync(id);
        if (existingItem == null)
            return NotFound();

        var categoryExists = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
        if (categoryExists == null)
            return BadRequest($"Category with id {dto.CategoryId} does not exist.");

        var duplicateName = await _unitOfWork.InventoryItems.FindAsync(i => i.Name.ToLower() == dto.Name.ToLower() && i.Id != id);
        if (duplicateName.Any())
            return BadRequest(new { message = $"An inventory item with the name '{dto.Name}' already exists." });
            
        var duplicateBarcode = await _unitOfWork.InventoryItems.FindAsync(i => i.Barcode == dto.Barcode && i.Id != id);
        if (duplicateBarcode.Any())
            return BadRequest(new { message = $"An inventory item with the barcode '{dto.Barcode}' already exists." });

        existingItem.Name = dto.Name;
        existingItem.CategoryId = dto.CategoryId;
        existingItem.Barcode = dto.Barcode;
        existingItem.CurrentStock = dto.CurrentStock;
        existingItem.CriticalThreshold = dto.CriticalThreshold;

        _unitOfWork.InventoryItems.Update(existingItem);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _unitOfWork.InventoryItems.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        _unitOfWork.InventoryItems.Remove(item);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private static InventoryResponseDto MapToResponseDto(InventoryItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        CategoryId = item.CategoryId,
        CategoryName = item.Category?.Name ?? "Unknown",
        Barcode = item.Barcode,
        CurrentStock = item.CurrentStock,
        CriticalThreshold = item.CriticalThreshold,
        IsBelowCriticalThreshold = item.CurrentStock < item.CriticalThreshold
    };
}