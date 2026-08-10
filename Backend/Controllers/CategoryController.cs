using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskInventoryApi.Models;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        return Ok(categories.Select(c => new CategoryResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create(CategoryCreateDto dto)
    {
        var existingCategory = await _unitOfWork.Categories.FindAsync(c => c.Name.ToLower() == dto.Name.ToLower());
        if (existingCategory.Any())
        {
            return BadRequest(new { message = $"A category with the name '{dto.Name}' already exists." });
        }

        var category = new Category
        {
            Name = dto.Name,
            Description = dto.Description
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = category.Id }, new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CategoryUpdateDto dto)
    {
        var existing = await _unitOfWork.Categories.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var duplicateCategory = await _unitOfWork.Categories.FindAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != id);
        if (duplicateCategory.Any())
        {
            return BadRequest(new { message = $"A category with the name '{dto.Name}' already exists." });
        }

        existing.Name = dto.Name;
        existing.Description = dto.Description;

        _unitOfWork.Categories.Update(existing);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _unitOfWork.Categories.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        _unitOfWork.Categories.Remove(existing);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}
