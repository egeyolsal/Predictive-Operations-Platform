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
        var items = await _unitOfWork.InventoryItems.GetAllAsync();
        return Ok(items.Select(MapToResponseDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryResponseDto>> GetById(int id)
    {
        var item = await _unitOfWork.InventoryItems.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        return Ok(MapToResponseDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<InventoryResponseDto>> Create(InventoryCreateDto dto)
    {
        var item = new InventoryItem
        {
            Name = dto.Name,
            Category = dto.Category,
            CurrentStock = dto.CurrentStock,
            CriticalThreshold = dto.CriticalThreshold
        };

        await _unitOfWork.InventoryItems.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, MapToResponseDto(item));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, InventoryUpdateDto dto)
    {
        var existingItem = await _unitOfWork.InventoryItems.GetByIdAsync(id);
        if (existingItem == null)
            return NotFound();

        existingItem.Name = dto.Name;
        existingItem.Category = dto.Category;
        existingItem.CurrentStock = dto.CurrentStock;
        existingItem.CriticalThreshold = dto.CriticalThreshold;

        _unitOfWork.InventoryItems.Update(existingItem);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

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
        Category = item.Category,
        CurrentStock = item.CurrentStock,
        CriticalThreshold = item.CriticalThreshold
    };
}