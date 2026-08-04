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