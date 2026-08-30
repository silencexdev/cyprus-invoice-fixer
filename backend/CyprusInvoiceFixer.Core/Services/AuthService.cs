using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CyprusInvoiceFixer.Core.Data;
using CyprusInvoiceFixer.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CyprusInvoiceFixer.Core.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(AppDbContext db, IConfiguration configuration)
    {
        _db          = db;
        _jwtSecret   = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT_SECRET is required");
        _jwtIssuer   = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? "CyprusInvoiceFixer";
        _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CyprusInvoiceFixerUsers";
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string? fullName, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email.ToLower(), ct))
            return new AuthResult { Success = false, Error = "Email already in use." };

        var user = new User
        {
            Email        = email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName     = fullName
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return new AuthResult { Success = true, Token = GenerateToken(user), UserId = user.Id };
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return new AuthResult { Success = false, Error = "Invalid email or password." };

        return new AuthResult { Success = true, Token = GenerateToken(user), UserId = user.Id };
    }

    private string GenerateToken(User user)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("plan",                        user.Plan.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(_jwtIssuer, _jwtAudience, claims,
            expires: DateTime.UtcNow.AddDays(30), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
