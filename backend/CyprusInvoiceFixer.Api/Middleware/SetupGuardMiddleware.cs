namespace CyprusInvoiceFixer.Api.Middleware;

public class SetupGuardMiddleware
{
    private static readonly string SetupFlagPath = Path.Combine(AppContext.BaseDirectory, ".setup_complete");
    private readonly RequestDelegate _next;

    public SetupGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var isSetupRoute = path.StartsWith("/api/setup", StringComparison.OrdinalIgnoreCase);
        var isHealthRoute = path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);

        if (!isSetupRoute && !isHealthRoute && !File.Exists(SetupFlagPath))
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Application not configured. Please complete the setup wizard at /setup.",
                setupUrl = "/setup"
            });
            return;
        }

        await _next(context);
    }
}
