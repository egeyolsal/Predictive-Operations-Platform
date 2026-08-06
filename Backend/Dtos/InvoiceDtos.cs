using System.ComponentModel.DataAnnotations;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Dtos;

public class InvoiceLineItemCreateDto
{
    [Required]
    public int InventoryItemId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public class InvoiceCreateDto
{
    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    [Required]
    public InvoiceType Type { get; set; }

    // Can be null for Internal Consumption
    public int? CustomerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "An invoice must contain at least one line item.")]
    public List<InvoiceLineItemCreateDto> LineItems { get; set; } = new();
}

public class InvoiceLineItemResponseDto
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public string InventoryItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class InvoiceResponseDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    
    public List<InvoiceLineItemResponseDto> LineItems { get; set; } = new();
}
