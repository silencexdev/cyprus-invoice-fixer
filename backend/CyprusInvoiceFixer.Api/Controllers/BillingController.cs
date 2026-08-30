using System.Security.Claims;
using CyprusInvoiceFixer.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyprusInvoiceFixer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IStripeService _stripe;

    public BillingController(IStripeService stripe) => _stripe = stripe;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException());

    /// <summary>Create a Stripe checkout session to upgrade to Paid plan.</summary>
    [Authorize]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest req, CancellationToken ct)
    {
        var url = await _stripe.CreateCheckoutSessionAsync(UserId, req.SuccessUrl, req.CancelUrl, ct);
        return Ok(new { url });
    }

    /// <summary>Stripe webhook endpoint — receives payment events.</summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        var sig = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

        try
        {
            await _stripe.HandleWebhookAsync(payload, sig, ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CheckoutRequest(string SuccessUrl, string CancelUrl);
