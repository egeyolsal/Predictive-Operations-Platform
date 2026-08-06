namespace TaskInventoryApi.Models;

public enum InvoiceType
{
    Inbound,             // Giriş / Purchase
    Outbound,            // Çıkış / Sale
    InternalConsumption  // İç Tüketim
}
