using CyprusInvoiceFixer.Core.Models;

namespace CyprusInvoiceFixer.Core.Services;

public interface IPdfService
{
    byte[] GenerateInvoicePdf(Invoice invoice);
}
