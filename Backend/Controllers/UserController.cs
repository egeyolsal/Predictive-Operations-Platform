using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public UserController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return Ok(users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-list")]
    public async Task<ActionResult<IEnumerable<UserAdminListDto>>> GetAdminList()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return Ok(users.Select(u => new UserAdminListDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role.ToString(),
            ProfilePictureUrl = u.ProfilePictureUrl
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/role")]
    public async Task<ActionResult> UpdateRole(int id, UpdateUserRoleDto dto)
    {
        var targetUser = await _unitOfWork.Users.GetByIdAsync(id);
        if (targetUser == null)
            return NotFound("User not found.");

        var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserIdString) || !int.TryParse(currentUserIdString, out int currentUserId))
            return Unauthorized();

        // Admin cannot change their own role (prevent accidental lockout)
        if (currentUserId == id)
            return BadRequest("You cannot change your own role.");

        if (!Enum.TryParse<UserRole>(dto.Role, out var newRole))
            return BadRequest("Invalid role specified.");

        targetUser.Role = newRole;
        _unitOfWork.Users.Update(targetUser);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "User role updated successfully." });
    }
    [Authorize(Roles = "Admin")]
    [HttpPost("admin-create")]
    public async Task<ActionResult> AdminCreateUser(AdminCreateUserDto dto)
    {
        var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Username == dto.Username || u.Email == dto.Email);
        if (existingUsers.Any())
            return BadRequest("This username or email is already taken.");

        if (!Enum.TryParse<UserRole>(dto.Role, out var newRole))
            return BadRequest("Invalid role specified.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = newRole
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "User created successfully." });
    }
}
