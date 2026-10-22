using PdfSharp.Pdf.IO;
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Services;
using Xunit;

namespace SaborExpressBack.InvoicePdf.Tests;

public sealed class InvoicePdfGeneratorTests
{
    [Theory]
    [InlineData("es-CO", "Factura / comprobante")]
    [InlineData("en-US", "Invoice / receipt")]
    [InlineData("pt-BR", "Fatura / comprovante")]
    [InlineData("fr-FR", "Facture / reçu")]
    [InlineData("de-DE", "Factura / comprobante")]
    public void Generate_ReturnsReadablePdfForSupportedLanguages(string language, string localizedTitle)
    {
        var generator = new InvoicePdfGenerator();
        var bytes = generator.Generate(SampleInvoice(), language);

        Assert.True(bytes.Length > 500);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));

        using var stream = new MemoryStream(bytes);
        var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.Equal(1, pdf.PageCount);
        Assert.Contains(localizedTitle, pdf.Info.Title);
        Assert.Contains("QA-FACTURA-2048", pdf.Info.Title);
    }

    [Fact]
    public void Generate_PaginatesInvoicesWithManyItems()
    {
        var invoice = SampleInvoice();
        invoice.Items = Enumerable.Range(1, 100)
            .Select(index => new InvoiceItemResponseDto
            {
                Name = $"Producto sintético {index}: descripción extensa para validar salto automático de página",
                Quantity = 2,
                UnitPrice = 1450m,
                Total = 2900m,
                Notes = "Sin ingredientes adicionales"
            })
            .ToList();

        var bytes = new InvoicePdfGenerator().Generate(invoice, "es");

        using var stream = new MemoryStream(bytes);
        var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 1);
    }

    [Fact]
    public void Generate_RendersVoidedReceiptWithReason()
    {
        var invoice = SampleInvoice();
        invoice.IsVoided = true;
        invoice.VoidReason = "Prueba sintética de anulación";

        var bytes = new InvoicePdfGenerator().Generate(invoice, "es");

        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    private static InvoiceResponseDto SampleInvoice() => new()
    {
        Id = 2048,
        InvoiceNumber = "QA-FACTURA-2048",
        PaymentId = 1300,
        OrderId = 901,
        BranchId = 5,
        BranchName = "Sede sintética Centro",
        BranchAddress = "Calle de prueba 123",
        BranchPhone = "+57 300 000 0000",
        CustomerName = "Cliente de prueba",
        CustomerDocument = "DOC-QA-01",
        CustomerEmail = "qa@example.invalid",
        DeliveryAddressLine = "Dirección sintética",
        DeliveryAddressReference = "Torre de prueba",
        OrderType = "Delivery",
        CashierName = "Cajero de prueba",
        PaymentMethod = "Cash",
        PaymentReference = "QA-PAYMENT-1300",
        PaymentAmount = 18300m,
        PaidAt = new DateTime(2026, 10, 22, 17, 30, 0, DateTimeKind.Utc),
        Items =
        [
            new InvoiceItemResponseDto
            {
                Name = "Arepa con queso",
                Quantity = 2,
                UnitPrice = 8000m,
                Total = 16000m,
                Notes = "Sin cebolla",
                IsToGo = true
            }
        ],
        SubTotal = 16000m,
        Tax = 1280m,
        TaxRate = 8m,
        TaxIncludedInPrice = false,
        DeliveryFee = 1020m,
        Total = 18300m,
        IssuedAt = new DateTime(2026, 10, 22, 17, 20, 0, DateTimeKind.Utc)
    };
}
