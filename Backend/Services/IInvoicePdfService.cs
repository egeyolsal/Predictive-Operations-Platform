using TaskInventoryApi.Models;

namespace TaskInventoryApi.Services;

public interface IInvoicePdfService
{
    byte[] GenerateInvoicePdf(Invoice invoice, Customer? customer, Supplier? supplier);
}
