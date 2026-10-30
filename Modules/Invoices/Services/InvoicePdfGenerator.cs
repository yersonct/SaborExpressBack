using System.Globalization;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Interfaces;

namespace SaborExpress.Modules.Invoices.Services;

public sealed class InvoicePdfGenerator : IInvoicePdfGenerator
{
    private static readonly Lazy<bool> FontResolverConfigured = new(() =>
    {
        GlobalFontSettings.FontResolver = new InvoiceFontResolver();
        return true;
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    public byte[] Generate(InvoiceResponseDto invoice, string? language = null)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        _ = FontResolverConfigured.Value;

        var labels = InvoicePdfLabels.For(language);
        var document = CreateDocument(invoice, labels);
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();

        using var output = new MemoryStream();
        renderer.PdfDocument.Save(output);
        return output.ToArray();
    }

    private static Document CreateDocument(InvoiceResponseDto invoice, InvoicePdfLabels labels)
    {
        var document = new Document();
        document.Info.Title = $"{labels.Title} {invoice.InvoiceNumber}";
        document.Info.Subject = $"{labels.Order} {invoice.OrderId}";

        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = InvoiceFontResolver.FamilyName;
        normal.Font.Size = 9;

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.7);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.7);

        var brand = section.AddParagraph("SaborExpress");
        brand.Format.Font.Size = 18;
        brand.Format.Font.Bold = true;
        brand.Format.SpaceAfter = Unit.FromPoint(2);

        AddOptionalParagraph(section, invoice.BranchName);
        AddOptionalParagraph(section, invoice.BranchAddress);
        AddOptionalParagraph(section, invoice.BranchPhone);

        var title = section.AddParagraph(labels.Title);
        title.Format.Font.Size = 15;
        title.Format.Font.Bold = true;
        title.Format.SpaceBefore = Unit.FromPoint(12);
        title.Format.SpaceAfter = Unit.FromPoint(6);

        AddField(section, labels.InvoiceNumber, invoice.InvoiceNumber);
        AddField(section, labels.IssuedAt, FormatBogotaDate(invoice.IssuedAt));
        AddField(section, labels.Order, invoice.OrderId.ToString(CultureInfo.InvariantCulture));
        AddField(section, labels.OrderType, LocalizeOrderType(invoice.OrderType, labels));
        if (invoice.TableNumber.HasValue)
            AddField(section, labels.Table, invoice.TableNumber.Value.ToString(CultureInfo.InvariantCulture));

        AddSectionTitle(section, labels.Customer);
        AddField(section, labels.CustomerName, invoice.CustomerName ?? labels.GeneralCustomer);
        AddOptionalField(section, labels.Document, invoice.CustomerDocument);
        AddOptionalField(section, labels.Email, invoice.CustomerEmail);
        AddOptionalField(section, labels.DeliveryAddress, invoice.DeliveryAddressLine);
        AddOptionalField(section, labels.AddressReference, invoice.DeliveryAddressReference);

        AddSectionTitle(section, labels.Items);
        AddItemsTable(section, invoice.Items ?? [], labels);

        AddTotalsTable(section, invoice, labels);

        AddSectionTitle(section, labels.Payment);
        AddField(section, labels.PaymentMethod, LocalizePaymentMethod(invoice.PaymentMethod, labels));
        AddOptionalField(section, labels.PaymentReference, invoice.PaymentReference);
        AddField(section, labels.PaidAmount, FormatMoney(invoice.PaymentAmount));
        AddField(section, labels.PaidAt, invoice.PaidAt.HasValue
            ? FormatBogotaDate(invoice.PaidAt.Value)
            : labels.NotAvailable);
        AddOptionalField(section, labels.Cashier, invoice.CashierName);

        if (invoice.IsVoided)
        {
            var voided = section.AddParagraph(labels.Voided);
            voided.Format.Font.Bold = true;
            voided.Format.SpaceBefore = Unit.FromPoint(10);
            AddOptionalField(section, labels.VoidReason, invoice.VoidReason);
        }

        var footer = section.Footers.Primary.AddParagraph(labels.GeneratedBy);
        footer.Format.Font.Size = 8;
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddText(" · ");
        footer.AddPageField();

        return document;
    }

    private static void AddItemsTable(Section section, IReadOnlyList<InvoiceItemResponseDto> items, InvoicePdfLabels labels)
    {
        var table = section.AddTable();
        table.Borders.Bottom.Width = Unit.FromPoint(0.35);
        table.Borders.Color = Colors.LightGray;
        table.TopPadding = Unit.FromPoint(4);
        table.BottomPadding = Unit.FromPoint(4);
        table.AddColumn(Unit.FromCentimeter(8.3));
        var quantityColumn = table.AddColumn(Unit.FromCentimeter(1.4));
        quantityColumn.Format.Alignment = ParagraphAlignment.Center;
        var unitPriceColumn = table.AddColumn(Unit.FromCentimeter(3.4));
        unitPriceColumn.Format.Alignment = ParagraphAlignment.Right;
        var totalColumn = table.AddColumn(Unit.FromCentimeter(3.5));
        totalColumn.Format.Alignment = ParagraphAlignment.Right;

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Colors.LightGray;
        AddBoldCell(header.Cells[0], labels.Product);
        AddBoldCell(header.Cells[1], labels.Quantity);
        AddBoldCell(header.Cells[2], labels.UnitPrice);
        AddBoldCell(header.Cells[3], labels.Total);

        foreach (var item in items)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(item.Name);
            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                var notes = row.Cells[0].AddParagraph(item.Notes);
                notes.Format.Font.Size = 8;
            }
            if (item.IsToGo)
            {
                var toGo = row.Cells[0].AddParagraph(labels.ToGo);
                toGo.Format.Font.Size = 8;
            }

            row.Cells[1].AddParagraph(item.Quantity.ToString(CultureInfo.InvariantCulture));
            row.Cells[2].AddParagraph(FormatMoney(item.UnitPrice));
            row.Cells[3].AddParagraph(FormatMoney(item.Total));
        }

        if (items.Count == 0)
        {
            var empty = table.AddRow();
            empty.Cells[0].MergeRight = 3;
            empty.Cells[0].AddParagraph(labels.NoItems);
        }
    }

    private static void AddTotalsTable(Section section, InvoiceResponseDto invoice, InvoicePdfLabels labels)
    {
        var table = section.AddTable();
        table.Rows.LeftIndent = Unit.FromCentimeter(8.3);
        table.AddColumn(Unit.FromCentimeter(5.8));
        var amountColumn = table.AddColumn(Unit.FromCentimeter(3.5));
        amountColumn.Format.Alignment = ParagraphAlignment.Right;

        AddTotalRow(table, labels.Subtotal, FormatMoney(invoice.SubTotal));
        var taxLabel = string.Format(CultureInfo.InvariantCulture, labels.Tax, invoice.TaxRate);
        if (invoice.TaxIncludedInPrice)
            taxLabel += $" ({labels.Included})";
        AddTotalRow(table, taxLabel, FormatMoney(invoice.Tax));
        AddTotalRow(table, labels.DeliveryFee, FormatMoney(invoice.DeliveryFee));
        var total = AddTotalRow(table, labels.Total, FormatMoney(invoice.Total));
        total.Format.Font.Bold = true;
        total.Format.Font.Size = 11;
    }

    private static Row AddTotalRow(Table table, string label, string value)
    {
        var row = table.AddRow();
        row.Cells[0].AddParagraph(label);
        row.Cells[1].AddParagraph(value);
        return row;
    }

    private static void AddBoldCell(Cell cell, string value)
    {
        var paragraph = cell.AddParagraph(value);
        paragraph.Format.Font.Bold = true;
    }

    private static void AddSectionTitle(Section section, string text)
    {
        var paragraph = section.AddParagraph(text);
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Size = 11;
        paragraph.Format.SpaceBefore = Unit.FromPoint(10);
        paragraph.Format.SpaceAfter = Unit.FromPoint(4);
        paragraph.Format.KeepWithNext = true;
    }

    private static void AddField(Section section, string label, string value)
    {
        var paragraph = section.AddParagraph();
        paragraph.Format.SpaceAfter = Unit.FromPoint(2);
        paragraph.AddFormattedText($"{label}: ", TextFormat.Bold);
        paragraph.AddText(string.IsNullOrWhiteSpace(value) ? "-" : value);
    }

    private static void AddOptionalParagraph(Section section, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            section.AddParagraph(value);
    }

    private static void AddOptionalField(Section section, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            AddField(section, label, value);
    }

    private static string FormatMoney(decimal? amount)
        => amount.HasValue
            ? $"$ {amount.Value.ToString("N2", CultureInfo.GetCultureInfo("es-CO"))}"
            : "-";

    private static string FormatBogotaDate(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        var zoneId = OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota";
        var bogotaTime = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(zoneId));
        return bogotaTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " COT";
    }

    private static string LocalizeOrderType(string? orderType, InvoicePdfLabels labels) => orderType switch
    {
        "DineIn" => labels.DineIn,
        "ToGo" => labels.ToGo,
        "Delivery" => labels.Delivery,
        "Pickup" => labels.Pickup,
        _ => orderType ?? labels.NotAvailable
    };

    private static string LocalizePaymentMethod(string? method, InvoicePdfLabels labels)
    {
        if (string.IsNullOrWhiteSpace(method))
            return labels.NotAvailable;

        return method.ToUpperInvariant() switch
        {
            "CASH" => labels.Cash,
            "CARD" => labels.Card,
            "TRANSFER" => labels.TransferMethod,
            "WOMPI" => "Wompi",
            _ => method
        };
    }
}

internal sealed class InvoiceFontResolver : IFontResolver
{
    public const string FamilyName = "SaborExpress Sans";
    private const string RegularFace = "SaborExpress-Regular";
    private const string BoldFace = "SaborExpress-Bold";
    private static readonly byte[] RegularFont = ReadFont("SaborExpress.Modules.Invoices.Fonts.Carlito-Regular.ttf");
    private static readonly byte[] BoldFont = ReadFont("SaborExpress.Modules.Invoices.Fonts.Carlito-Bold.ttf");

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // PDFsharp/MigraDoc requests Courier New for its internal error font. Map
        // every requested family to the embedded font so rendering is portable
        // and does not depend on fonts installed in the host container.
        return new FontResolverInfo(isBold ? BoldFace : RegularFace, mustSimulateBold: false, mustSimulateItalic: isItalic);
    }

    public byte[]? GetFont(string faceName) => faceName switch
    {
        RegularFace => RegularFont,
        BoldFace => BoldFont,
        _ => null
    };

    private static byte[] ReadFont(string resourceName)
    {
        using var stream = typeof(InvoiceFontResolver).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new SaborExpress.Shared.Exceptions.ConflictException($"No se encontró el recurso de fuente requerido: {resourceName}");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}

internal sealed record InvoicePdfLabels(
    string Title,
    string InvoiceNumber,
    string IssuedAt,
    string Order,
    string OrderType,
    string Table,
    string Customer,
    string CustomerName,
    string GeneralCustomer,
    string Document,
    string Email,
    string DeliveryAddress,
    string AddressReference,
    string Items,
    string Product,
    string Quantity,
    string UnitPrice,
    string Total,
    string NoItems,
    string ToGo,
    string Subtotal,
    string Tax,
    string Included,
    string DeliveryFee,
    string Payment,
    string PaymentMethod,
    string PaymentReference,
    string PaidAmount,
    string PaidAt,
    string Cashier,
    string Voided,
    string VoidReason,
    string GeneratedBy,
    string NotAvailable,
    string DineIn,
    string Delivery,
    string Pickup,
    string Cash,
    string Card,
    string TransferMethod)
{
    public static InvoicePdfLabels For(string? language)
    {
        var normalized = language?.Split(',', ';')[0].Trim().Split('-')[0].ToLowerInvariant();
        return normalized switch
        {
            "en" => English,
            "pt" => Portuguese,
            "fr" => French,
            _ => Spanish
        };
    }

    private static InvoicePdfLabels Spanish => new(
        "Factura / comprobante", "Número", "Fecha de emisión", "Pedido", "Tipo de pedido", "Mesa",
        "Cliente", "Nombre", "Cliente general", "Documento", "Correo", "Dirección de entrega", "Referencia",
        "Productos", "Producto", "Cant.", "Precio unitario", "Total", "Sin productos", "Para llevar",
        "Subtotal", "Impuesto ({0:0.##}%)", "incluido en precio", "Domicilio", "Pago", "Medio de pago", "Referencia del pago",
        "Valor pagado", "Fecha de pago", "Cajero", "COMPROBANTE ANULADO", "Motivo de anulación", "Generado por SaborExpress",
        "No disponible", "En mesa", "Domicilio", "Recoger en local", "Efectivo", "Tarjeta", "Transferencia");

    private static InvoicePdfLabels English => new(
        "Invoice / receipt", "Number", "Issue date", "Order", "Order type", "Table",
        "Customer", "Name", "Walk-in customer", "Document", "Email", "Delivery address", "Reference",
        "Items", "Product", "Qty.", "Unit price", "Total", "No items", "To go",
        "Subtotal", "Tax ({0:0.##}%)", "included in price", "Delivery fee", "Payment", "Payment method", "Payment reference",
        "Amount paid", "Payment date", "Cashier", "VOIDED RECEIPT", "Void reason", "Generated by SaborExpress",
        "Not available", "Dine in", "Delivery", "Pickup", "Cash", "Card", "Bank transfer");

    private static InvoicePdfLabels Portuguese => new(
        "Fatura / comprovante", "Número", "Data de emissão", "Pedido", "Tipo de pedido", "Mesa",
        "Cliente", "Nome", "Cliente geral", "Documento", "E-mail", "Endereço de entrega", "Referência",
        "Produtos", "Produto", "Qtd.", "Preço unitário", "Total", "Sem produtos", "Para levar",
        "Subtotal", "Imposto ({0:0.##}%)", "incluído no preço", "Taxa de entrega", "Pagamento", "Forma de pagamento", "Referência do pagamento",
        "Valor pago", "Data do pagamento", "Caixa", "COMPROVANTE ANULADO", "Motivo do cancelamento", "Gerado por SaborExpress",
        "Não disponível", "No local", "Entrega", "Retirada", "Dinheiro", "Cartão", "Transferência");

    private static InvoicePdfLabels French => new(
        "Facture / reçu", "Numéro", "Date d’émission", "Commande", "Type de commande", "Table",
        "Client", "Nom", "Client comptoir", "Document", "E-mail", "Adresse de livraison", "Référence",
        "Articles", "Produit", "Qté", "Prix unitaire", "Total", "Aucun article", "À emporter",
        "Sous-total", "Taxe ({0:0.##}%)", "incluse dans le prix", "Frais de livraison", "Paiement", "Mode de paiement", "Référence de paiement",
        "Montant payé", "Date de paiement", "Caissier", "REÇU ANNULÉ", "Motif de l’annulation", "Généré par SaborExpress",
        "Indisponible", "Sur place", "Livraison", "À récupérer", "Espèces", "Carte", "Virement bancaire");
}
