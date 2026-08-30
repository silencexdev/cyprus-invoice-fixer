using CyprusInvoiceFixer.Core.Models;

namespace CyprusInvoiceFixer.Core.Services;

public interface IInvoiceService
{
    Task<Invoice> ParseAndSaveAsync(Guid userId, string rawText, CancellationToken ct = default);
    Task<Invoice> ParseImageAndSaveAsync(Guid userId, byte[] imageBytes, string mimeType, CancellationToken ct = default);
    Task<List<Invoice>> GetUserInvoicesAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(Guid userId, Guid invoiceId, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, Guid invoiceId, CancellationToken ct = default);
    Task<List<ValidationIssue>> ValidateAsync(Invoice invoice);
}
