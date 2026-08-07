namespace TaskInventoryApi.Models;

public class InventoryItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? Barcode { get; set; }

    public int CurrentStock { get; set; }

    public int CriticalThreshold { get; set; }

    // Navigation property
    public ICollection<ItemSupplier> ItemSuppliers { get; set; } = new List<ItemSupplier>();
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<InvoiceLineItem> InvoiceLineItems { get; set; } = new List<InvoiceLineItem>();
}