using System.ComponentModel.DataAnnotations;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Dtos;

public class ProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format.")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Password must be at least 8 characters long, contain at least 1 uppercase letter and 1 number.")]
    public string NewPassword { get; set; } = string.Empty;
}
