using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskInventoryApi.Models;

public enum UserRole
{
    Admin,
    Analyst,
    Worker
}

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Worker;

    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }

    public string? ProfilePictureUrl { get; set; }

    // Navigation property
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
}