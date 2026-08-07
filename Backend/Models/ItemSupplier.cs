namespace TaskInventoryApi.Models;

public class ItemSupplier
{
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public decimal Price { get; set; }
    
    public int LeadTimeDays { get; set; }
}
