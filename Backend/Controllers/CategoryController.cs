using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        return Ok(categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        }));
    }
}
