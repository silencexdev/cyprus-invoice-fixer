using System.ComponentModel.DataAnnotations;

namespace CyprusInvoiceFixer.Core.Models;

public class InvoiceLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [MaxLength(500)] public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}
