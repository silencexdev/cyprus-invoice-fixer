using CyprusInvoiceFixer.Core.Data;
using CyprusInvoiceFixer.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace CyprusInvoiceFixer.Core.Services;

public class StripeService : IStripeService
{
    private readonly AppDbContext _db;
    private readonly string _webhookSecret;

    public StripeService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        var stripeKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("STRIPE_SECRET_KEY is required");
        _webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
            ?? configuration["Stripe:WebhookSecret"] ?? string.Empty;
        StripeConfiguration.ApiKey = stripeKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid userId, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode          = "subscription",
            CustomerEmail = user.Email,
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price    = Environment.GetEnvironmentVariable("STRIPE_PRICE_ID")
                                ?? throw new InvalidOperationException("STRIPE_PRICE_ID is required"),
                    Quantity = 1
                }
            },
            Metadata   = new Dictionary<string, string> { { "userId", userId.ToString() } },
            SuccessUrl = successUrl,
            CancelUrl  = cancelUrl
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    public async Task HandleWebhookAsync(string payload, string stripeSignature, CancellationToken ct = default)
    {
        var stripeEvent = EventUtility.ConstructEvent(payload, stripeSignature, _webhookSecret);
        if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session?.Metadata.TryGetValue("userId", out var userIdStr) == true
                && Guid.TryParse(userIdStr, out var userId))
            {
                var user = await _db.Users.FindAsync(new object[] { userId }, ct);
                if (user != null)
                {
                    user.Plan = UserPlan.Paid;
                    await _db.SaveChangesAsync(ct);
                }
            }
        }
    }
}
