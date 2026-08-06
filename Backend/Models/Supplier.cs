using System.ComponentModel.DataAnnotations;

namespace TaskInventoryApi.Models;

public class Supplier
{
    public int Id { get; set; }

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

    // Navigation property
    public ICollection<ItemSupplier> ItemSuppliers { get; set; } = new List<ItemSupplier>();
}
