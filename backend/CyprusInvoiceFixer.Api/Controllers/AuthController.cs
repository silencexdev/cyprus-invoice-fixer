using CyprusInvoiceFixer.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CyprusInvoiceFixer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Register a new user.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req.Email, req.Password, req.FullName, ct);
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        return Ok(new { token = result.Token, userId = result.UserId });
    }

    /// <summary>Login and receive a JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req.Email, req.Password, ct);
        if (!result.Success)
            return Unauthorized(new { error = result.Error });
        return Ok(new { token = result.Token, userId = result.UserId });
    }
}

public record RegisterRequest(string Email, string Password, string? FullName);
public record LoginRequest(string Email, string Password);
