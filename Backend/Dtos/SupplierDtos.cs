using System.ComponentModel.DataAnnotations;

namespace TaskInventoryApi.Dtos;

public class SupplierCreateDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }
}

public class SupplierResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class ItemSupplierAssignDto
{
    [Required]
    public int InventoryItemId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }
}
