using System.ComponentModel.DataAnnotations;

namespace TaskInventoryApi.Dtos;

public class InventoryCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [Range(0, int.MaxValue)]
    public int CurrentStock { get; set; }
    
    [Range(0, int.MaxValue)]
    public int CriticalThreshold { get; set; }
}

public class InventoryUpdateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [Range(0, int.MaxValue)]
    public int CurrentStock { get; set; }

    [Range(0, int.MaxValue)]
    public int CriticalThreshold { get; set; }
}

public class InventoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int CurrentStock { get; set; }
    public int CriticalThreshold { get; set; }
    public bool IsBelowCriticalThreshold { get; set; }
}