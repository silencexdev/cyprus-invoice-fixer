namespace CyprusInvoiceFixer.Core.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(Guid userId, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string stripeSignature, CancellationToken ct = default);
}
