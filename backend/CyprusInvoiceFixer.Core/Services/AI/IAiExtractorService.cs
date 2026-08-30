namespace CyprusInvoiceFixer.Core.Services.AI;

public interface IAiExtractorService
{
    Task<InvoiceExtractResult> ExtractFromTextAsync(string rawText, CancellationToken ct = default);
    Task<InvoiceExtractResult> ExtractFromImageBytesAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default);
}

public class InvoiceExtractResult
{
    public string? SupplierName { get; set; }
    public string? SupplierVatNumber { get; set; }
    public string? SupplierAddress { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerVatNumber { get; set; }
    public string? CustomerAddress { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? VatRate { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? Total { get; set; }
    public string Currency { get; set; } = "EUR";
    public List<LineItemExtract> LineItems { get; set; } = new();
    public string? AiNotes { get; set; }
}

public class LineItemExtract
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
