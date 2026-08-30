using System.ComponentModel.DataAnnotations;

namespace CyprusInvoiceFixer.Core.Models;

public class ValidationIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [MaxLength(100)] public string Field { get; set; } = string.Empty;
    [MaxLength(500)] public string Message { get; set; } = string.Empty;
    public IssueSeverity Severity { get; set; } = IssueSeverity.Error;
}

public enum IssueSeverity
{
    Warning = 0,
    Error = 1
}
