using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CyprusInvoiceFixer.Core.Services.AI;

public class OllamaExtractorService : IAiExtractorService
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaExtractorService(IConfiguration configuration)
    {
        var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")
            ?? configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model = Environment.GetEnvironmentVariable("OLLAMA_MODEL")
            ?? configuration["Ollama:Model"] ?? "llama3";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<InvoiceExtractResult> ExtractFromTextAsync(string rawText, CancellationToken ct = default)
    {
        var payload = new { model = _model, prompt = BuildPrompt(rawText), stream = false };
        var response = await _http.PostAsJsonAsync("/api/generate", payload, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct);
        return ParseResponse(body?.Response ?? "{}");
    }

    public async Task<InvoiceExtractResult> ExtractFromImageBytesAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default)
    {
        var text = OcrHelper.ExtractText(imageBytes);
        return await ExtractFromTextAsync(text, ct);
    }

    private static string BuildPrompt(string rawText) => $"""
        You are a Cyprus VAT invoice parser. Extract all invoice fields and return ONLY valid JSON.
        Fields: supplierName, supplierVatNumber, supplierAddress, customerName, customerVatNumber,
        customerAddress, invoiceNumber, invoiceDate, dueDate, subtotal, vatRate, vatAmount, total,
        currency, lineItems (array of description/quantity/unitPrice), aiNotes.
        Invoice text:
        {rawText}
        """;

    private static InvoiceExtractResult ParseResponse(string json)
    {
        try
        {
            var clean = json.Trim();
            if (clean.StartsWith("```")) clean = clean[(clean.IndexOf('\n') + 1)..];
            if (clean.EndsWith("```")) clean = clean[..clean.LastIndexOf("```")];
            return JsonSerializer.Deserialize<InvoiceExtractResult>(clean.Trim(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new InvoiceExtractResult();
        }
        catch { return new InvoiceExtractResult { AiNotes = "AI response could not be parsed." }; }
    }

    private class OllamaResponse { public string? Response { get; set; } }
}
