using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

namespace CyprusInvoiceFixer.Core.Services.AI;

public class OpenAiExtractorService : IAiExtractorService
{
    private readonly ChatClient _client;
    private const string Model = "gpt-4o-mini";

    public OpenAiExtractorService(IConfiguration configuration)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OPENAI_API_KEY is required");
        _client = new OpenAIClient(apiKey).GetChatClient(Model);
    }

    public async Task<InvoiceExtractResult> ExtractFromTextAsync(string rawText, CancellationToken ct = default)
    {
        var response = await _client.CompleteChatAsync(
            new[] { ChatMessage.CreateUserMessage(BuildPrompt(rawText)) }, cancellationToken: ct);
        return ParseResponse(response.Value.Content[0].Text);
    }

    public async Task<InvoiceExtractResult> ExtractFromImageBytesAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default)
    {
        var text = OcrHelper.ExtractText(imageBytes);
        return await ExtractFromTextAsync(text, ct);
    }

    private static string BuildPrompt(string rawText) => $$"""
        You are a Cyprus VAT invoice parser. Extract all invoice fields from the text below and return ONLY a JSON object.
        Required fields (use null if missing):
        supplierName, supplierVatNumber, supplierAddress,
        customerName, customerVatNumber, customerAddress,
        invoiceNumber, invoiceDate (ISO 8601), dueDate (ISO 8601),
        subtotal, vatRate (number e.g. 19), vatAmount, total, currency (default EUR),
        lineItems: [ { description, quantity, unitPrice } ],
        aiNotes (any observations or warnings about the invoice).
        Invoice text:
        {{rawText}}
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
}
