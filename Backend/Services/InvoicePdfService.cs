using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TaskInventoryApi.Models;

namespace TaskInventoryApi.Services;

public class InvoicePdfService : IInvoicePdfService
{
    public byte[] GenerateInvoicePdf(Invoice invoice, Customer? customer, Supplier? supplier)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, invoice, customer, supplier));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Worksight Predictive Platform").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("Predictive Operations & Inventory").FontSize(14).FontColor(Colors.Grey.Medium);
            });
            
            // Note: Currently using the absolute path of the AI-generated logo. 
            // In a production build, this should be moved to the wwwroot/Assets folder.
            var logoPath = @"C:\Users\HP\.gemini\antigravity-ide\brain\85e46967-3047-48cc-a006-08781b5c8113\worksight_logo_1786222978098.png";
            if (System.IO.File.Exists(logoPath))
            {
                row.ConstantItem(80).AlignRight().AlignTop().TranslateY(-10).TranslateX(10).Image(logoPath);
            }
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice, Customer? customer, Supplier? supplier)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(20);

            column.Item().Row(row =>
            {
                row.RelativeItem().Component(new AddressComponent("Bill To:", customer?.Name ?? supplier?.Name ?? "Internal Organization", customer?.Address ?? "N/A", customer?.Email ?? supplier?.Email ?? "N/A"));
                row.ConstantItem(50);
                row.RelativeItem().Component(new InvoiceDetailsComponent(invoice));
            });

            column.Item().Element(x => ComposeTable(x, invoice));

            var totalPrice = invoice.TotalAmount > 0 
                ? invoice.TotalAmount 
                : invoice.LineItems.Sum(li => li.Quantity * li.UnitPrice);

            column.Item().AlignRight().Text($"Total Amount: ${totalPrice:N2}").FontSize(14).SemiBold();
        });
    }

    private void ComposeTable(IContainer container, Invoice invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn(3);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("#");
                header.Cell().Element(CellStyle).Text("Item");
                header.Cell().Element(CellStyle).AlignRight().Text("Unit Price");
                header.Cell().Element(CellStyle).AlignRight().Text("Quantity");
                header.Cell().Element(CellStyle).AlignRight().Text("Total");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                }
            });

            int index = 1;
            foreach (var item in invoice.LineItems)
            {
                table.Cell().Element(CellStyle).Text(index.ToString());
                table.Cell().Element(CellStyle).Text(item.InventoryItem?.Name ?? "Unknown Item");
                table.Cell().Element(CellStyle).AlignRight().Text($"${item.UnitPrice:N2}");
                table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                table.Cell().Element(CellStyle).AlignRight().Text($"${item.UnitPrice * item.Quantity:N2}");

                index++;

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }
}

public class AddressComponent : IComponent
{
    private string Title { get; }
    private string Name { get; }
    private string Address { get; }
    private string Email { get; }

    public AddressComponent(string title, string name, string address, string email)
    {
        Title = title;
        Name = name;
        Address = address;
        Email = email;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().Text(Title).SemiBold();
            column.Item().Text(Name);
            column.Item().Text(Address);
            column.Item().Text(Email);
        });
    }
}

public class InvoiceDetailsComponent : IComponent
{
    private Invoice Invoice { get; }

    public InvoiceDetailsComponent(Invoice invoice)
    {
        Invoice = invoice;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().Text($"Invoice #{Invoice.InvoiceNumber}").FontSize(14).SemiBold();
            column.Item().Text($"Issue Date: {Invoice.InvoiceDate:d}");
            column.Item().Text($"Type: {Invoice.Type}");
        });
    }
}
