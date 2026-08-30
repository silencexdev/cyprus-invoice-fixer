using System.Security.Claims;
using CyprusInvoiceFixer.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyprusInvoiceFixer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoices;
    private readonly IPdfService _pdf;

    public InvoiceController(IInvoiceService invoices, IPdfService pdf)
    {
        _invoices = invoices;
        _pdf = pdf;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException());

    /// <summary>Parse invoice from raw text and save.</summary>
    [HttpPost("parse/text")]
    public async Task<IActionResult> ParseText([FromBody] ParseTextRequest req, CancellationToken ct)
    {
        try
        {
            var invoice = await _invoices.ParseAndSaveAsync(UserId, req.Text, ct);
            return Ok(invoice);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("limit"))
        {
            return StatusCode(429, new { error = ex.Message });
        }
    }

    /// <summary>Parse invoice from uploaded image (JPG/PNG/PDF).</summary>
    [HttpPost("parse/image")]
    public async Task<IActionResult> ParseImage(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        try
        {
            var invoice = await _invoices.ParseImageAndSaveAsync(UserId, bytes, file.ContentType, ct);
            return Ok(invoice);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("limit"))
        {
            return StatusCode(429, new { error = ex.Message });
        }
    }

    /// <summary>List all invoices for the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var invoices = await _invoices.GetUserInvoicesAsync(UserId, page, pageSize, ct);
        return Ok(invoices);
    }

    /// <summary>Get a single invoice by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var invoice = await _invoices.GetByIdAsync(UserId, id, ct);
        if (invoice == null) return NotFound();
        return Ok(invoice);
    }

    /// <summary>Delete an invoice.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _invoices.DeleteAsync(UserId, id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Download invoice as PDF.</summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var invoice = await _invoices.GetByIdAsync(UserId, id, ct);
        if (invoice == null) return NotFound();

        var pdfBytes = _pdf.GenerateInvoicePdf(invoice);
        var fileName = $"invoice-{invoice.InvoiceNumber ?? invoice.Id.ToString()[..8]}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>Re-validate an existing invoice and return issues.</summary>
    [HttpGet("{id:guid}/validate")]
    public async Task<IActionResult> Validate(Guid id, CancellationToken ct)
    {
        var invoice = await _invoices.GetByIdAsync(UserId, id, ct);
        if (invoice == null) return NotFound();

        var issues = await _invoices.ValidateAsync(invoice);
        return Ok(new
        {
            status = issues.Any(i => i.Severity == CyprusInvoiceFixer.Core.Models.IssueSeverity.Error)
                ? "Invalid" : "Valid",
            issues
        });
    }
}

public record ParseTextRequest(string Text);
