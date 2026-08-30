using System.ComponentModel.DataAnnotations;

namespace CyprusInvoiceFixer.Core.Models;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(200)] public string? SupplierName { get; set; }
    [MaxLength(20)]  public string? SupplierVatNumber { get; set; }
    [MaxLength(300)] public string? SupplierAddress { get; set; }

    [MaxLength(200)] public string? CustomerName { get; set; }
    [MaxLength(20)]  public string? CustomerVatNumber { get; set; }
    [MaxLength(300)] public string? CustomerAddress { get; set; }

    [MaxLength(50)]  public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal VatRate { get; set; } = 19m;
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }

    [MaxLength(3)] public string Currency { get; set; } = "EUR";

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string? RawInput { get; set; }
    public string? AiNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<ValidationIssue> ValidationIssues { get; set; } = new List<ValidationIssue>();
}

public enum InvoiceStatus
{
    Draft = 0,
    Valid = 1,
    Invalid = 2
}
