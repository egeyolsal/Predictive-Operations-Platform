using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Repositories;
using TaskInventoryApi.Services;
using BCrypt.Net;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public ProfileController(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        return Ok(new ProfileDto
        {
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString()
        });
    }

    [HttpPut]
    public async Task<ActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        // Check if new email is already taken by someone else
        if (user.Email != dto.Email)
        {
            var existingEmailUsers = await _unitOfWork.Users.FindAsync(u => u.Email == dto.Email);
            if (existingEmailUsers.Any())
                return BadRequest("This email is already in use by another account.");
        }

        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully." });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest("Incorrect current password.");

        // Hash and update new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Generate a new token so the frontend session continues seamlessly but securely
        var newToken = _tokenService.CreateToken(user);

        return Ok(new { 
            message = "Password changed successfully.",
            token = newToken 
        });
    }
}
