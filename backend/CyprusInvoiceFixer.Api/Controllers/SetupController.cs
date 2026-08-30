using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CyprusInvoiceFixer.Api.Controllers;

[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private static readonly string SetupFlagPath = Path.Combine(AppContext.BaseDirectory, ".setup_complete");
    private readonly IConfiguration _config;
    private readonly ILogger<SetupController> _log;

    public SetupController(IConfiguration config, ILogger<SetupController> log)
    {
        _config = config;
        _log = log;
    }

    [HttpGet("status")]
    public IActionResult Status() => Ok(new { configured = File.Exists(SetupFlagPath) });

    [HttpPost("configure")]
    public IActionResult Configure([FromBody] SetupRequest req)
    {
        if (File.Exists(SetupFlagPath))
            return BadRequest(new { error = "Already configured. Delete .setup_complete to reconfigure." });

        // Write appsettings.Production.json with user-supplied values
        var settings = new Dictionary<string, object>
        {
            ["ConnectionStrings"] = new Dictionary<string, string>
            {
                ["DefaultConnection"] = $"Host=db;Database=cyprusInvoiceFixer;Username=appuser;Password={req.PostgresPassword}"
            },
            ["Jwt"] = new Dictionary<string, string>
            {
                ["Secret"] = GenerateJwtSecret(),
                ["Issuer"] = "CyprusInvoiceFixer",
                ["Audience"] = "CyprusInvoiceFixerUsers"
            },
            ["Ai"] = new Dictionary<string, string?>
            {
                ["Provider"] = req.AiProvider,
                ["OpenAiKey"] = req.OpenAiKey,
                ["OllamaBaseUrl"] = req.OllamaUrl,
                ["OllamaModel"] = req.OllamaModel
            },
            ["Stripe"] = new Dictionary<string, string?>
            {
                ["SecretKey"] = req.StripeSecretKey,
                ["WebhookSecret"] = req.StripeWebhookSecret,
                ["PriceId"] = req.StripePriceId
            },
            ["App"] = new Dictionary<string, string?>
            {
                ["FrontendUrl"] = req.FrontendUrl
            }
        };

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        var outPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Production.json");
        System.IO.File.WriteAllText(outPath, json);
        System.IO.File.WriteAllText(SetupFlagPath, DateTime.UtcNow.ToString("O"));

        _log.LogInformation("First-run setup completed at {Time}", DateTime.UtcNow);
        return Ok(new { message = "Setup complete. Restart the API container to apply settings." });
    }

    private static string GenerateJwtSecret()
    {
        var bytes = new byte[48];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public record SetupRequest(
    string PostgresPassword,
    string AiProvider,
    string? OpenAiKey,
    string? OllamaUrl,
    string? OllamaModel,
    string? StripeSecretKey,
    string? StripeWebhookSecret,
    string? StripePriceId,
    string? FrontendUrl
);
