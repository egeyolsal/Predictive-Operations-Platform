using System.ComponentModel.DataAnnotations;

namespace TaskInventoryApi.Models;

public class Invoice
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public InvoiceType Type { get; set; }

    // CustomerId is nullable for Internal Consumption where there might be no customer
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public decimal TotalAmount { get; set; }

    // Navigation property
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}
