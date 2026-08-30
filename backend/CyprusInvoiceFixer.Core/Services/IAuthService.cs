namespace CyprusInvoiceFixer.Core.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string? fullName, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public Guid? UserId { get; set; }
}
