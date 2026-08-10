using Microsoft.AspNetCore.Mvc;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Models;
using TaskInventoryApi.Repositories;
using TaskInventoryApi.Services;

namespace TaskInventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{   
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthController(IUnitOfWork unitOfWork, ITokenService tokenService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Username == dto.Username || u.Email == dto.Email);
        if (existingUsers.Any())
            return BadRequest("This username or email is already taken.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Worker // güvenlik: kendi kendine Admin rolü seçemez
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = _tokenService.CreateToken(user);

        return Ok(new AuthResponseDto
        {
            Id = user.Id,
            Token = token,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.Username == dto.Username || u.Email == dto.Username);
        var user = users.FirstOrDefault();

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid username or password.");

        var token = _tokenService.CreateToken(user);

        return Ok(new AuthResponseDto
        {
            Id = user.Id,
            Token = token,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.Email == dto.Email);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            // Don't reveal that the user does not exist
            return Ok(new { message = "If the email is valid, a reset link has been sent." });
        }

        var resetToken = Guid.NewGuid().ToString();
        user.PasswordResetToken = resetToken;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Username, resetToken);

        return Ok(new { message = "If the email is valid, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.PasswordResetToken == dto.Token && u.ResetTokenExpires > DateTime.UtcNow);
        var user = users.FirstOrDefault();

        if (user == null)
            return BadRequest("Invalid or expired reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully." });
    }
}