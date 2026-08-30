using CyprusInvoiceFixer.Core.Data;
using CyprusInvoiceFixer.Core.Models;
using CyprusInvoiceFixer.Core.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace CyprusInvoiceFixer.Core.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly IAiExtractorService _ai;
    private const int FreeMonthlyLimit = 3;

    public InvoiceService(AppDbContext db, IAiExtractorService ai)
    {
        _db = db;
        _ai = ai;
    }

    public async Task<Invoice> ParseAndSaveAsync(Guid userId, string rawText, CancellationToken ct = default)
    {
        await EnforceUsageLimitAsync(userId, ct);
        var result = await _ai.ExtractFromTextAsync(rawText, ct);
        return await SaveExtractResultAsync(userId, rawText, result, ct);
    }

    public async Task<Invoice> ParseImageAndSaveAsync(Guid userId, byte[] imageBytes, string mimeType, CancellationToken ct = default)
    {
        await EnforceUsageLimitAsync(userId, ct);
        var result = await _ai.ExtractFromImageBytesAsync(imageBytes, mimeType, ct);
        return await SaveExtractResultAsync(userId, "[image upload]", result, ct);
    }

    public async Task<List<Invoice>> GetUserInvoicesAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await _db.Invoices
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(i => i.LineItems)
            .Include(i => i.ValidationIssues)
            .ToListAsync(ct);

    public async Task<Invoice?> GetByIdAsync(Guid userId, Guid invoiceId, CancellationToken ct = default)
        => await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.ValidationIssues)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.UserId == userId, ct);

    public async Task DeleteAsync(Guid userId, Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");
        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<ValidationIssue>> ValidateAsync(Invoice invoice)
    {
        var issues = new List<ValidationIssue>();
        void Add(string field, string msg, IssueSeverity sev = IssueSeverity.Error) =>
            issues.Add(new ValidationIssue { InvoiceId = invoice.Id, Field = field, Message = msg, Severity = sev });

        if (string.IsNullOrWhiteSpace(invoice.SupplierName))      Add("SupplierName",     "Supplier name is required.");
        if (string.IsNullOrWhiteSpace(invoice.SupplierVatNumber))  Add("SupplierVatNumber", "Supplier VAT number is required.");
        if (string.IsNullOrWhiteSpace(invoice.SupplierAddress))    Add("SupplierAddress",   "Supplier address is required.", IssueSeverity.Warning);
        if (string.IsNullOrWhiteSpace(invoice.CustomerName))       Add("CustomerName",      "Customer name is required.");
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))      Add("InvoiceNumber",     "Invoice number is required.");
        if (invoice.InvoiceDate == null)                            Add("InvoiceDate",       "Invoice date is required.");
        if (invoice.Total <= 0)                                     Add("Total",             "Total amount must be greater than zero.");
        if (invoice.VatRate != 5 && invoice.VatRate != 9 && invoice.VatRate != 19)
            Add("VatRate", $"VAT rate {invoice.VatRate}% is not a standard Cyprus rate (5%, 9%, 19%).", IssueSeverity.Warning);
        if (!invoice.LineItems.Any())
            Add("LineItems", "At least one line item is recommended.", IssueSeverity.Warning);

        return Task.FromResult(issues);
    }

    private async Task EnforceUsageLimitAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.UsageResetAt.Month != DateTime.UtcNow.Month || user.UsageResetAt.Year != DateTime.UtcNow.Year)
        {
            user.MonthlyUsageCount = 0;
            user.UsageResetAt = DateTime.UtcNow;
        }

        if (user.Plan == UserPlan.Free && user.MonthlyUsageCount >= FreeMonthlyLimit)
            throw new InvalidOperationException($"Free plan limit of {FreeMonthlyLimit} invoices/month reached.");

        user.MonthlyUsageCount++;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Invoice> SaveExtractResultAsync(Guid userId, string rawInput, InvoiceExtractResult r, CancellationToken ct)
    {
        var invoice = new Invoice
        {
            UserId            = userId,
            RawInput          = rawInput,
            SupplierName      = r.SupplierName,
            SupplierVatNumber = r.SupplierVatNumber,
            SupplierAddress   = r.SupplierAddress,
            CustomerName      = r.CustomerName,
            CustomerVatNumber = r.CustomerVatNumber,
            CustomerAddress   = r.CustomerAddress,
            InvoiceNumber     = r.InvoiceNumber,
            InvoiceDate       = r.InvoiceDate,
            DueDate           = r.DueDate,
            Subtotal          = r.Subtotal ?? 0,
            VatRate           = r.VatRate ?? 19,
            VatAmount         = r.VatAmount ?? 0,
            Total             = r.Total ?? 0,
            Currency          = r.Currency,
            AiNotes           = r.AiNotes,
            LineItems         = r.LineItems.Select(li => new InvoiceLineItem
            {
                Description = li.Description,
                Quantity    = li.Quantity,
                UnitPrice   = li.UnitPrice
            }).ToList()
        };

        var issues = await ValidateAsync(invoice);
        invoice.ValidationIssues = issues;
        invoice.Status = issues.Any(i => i.Severity == IssueSeverity.Error)
            ? InvoiceStatus.Invalid : InvoiceStatus.Valid;

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        return invoice;
    }
}
