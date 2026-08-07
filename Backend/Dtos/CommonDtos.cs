namespace TaskInventoryApi.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserAdminListDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserRoleDto
{
    public string Role { get; set; } = string.Empty;
}
