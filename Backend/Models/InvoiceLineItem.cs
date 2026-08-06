using System.ComponentModel.DataAnnotations;

namespace TaskInventoryApi.Models;

public class InvoiceLineItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    // Computed property for convenience (not mapped to DB, or can be mapped if preferred)
    public decimal TotalPrice => Quantity * UnitPrice;
}
