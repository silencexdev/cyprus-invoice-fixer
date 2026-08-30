using System.Security.Claims;
using CyprusInvoiceFixer.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyprusInvoiceFixer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly AppDbContext _db;

    public MeController(AppDbContext db) => _db = db;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException());

    /// <summary>Get current user profile and usage.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName,
            plan = user.Plan.ToString(),
            user.MonthlyUsageCount,
            usageResetAt = user.UsageResetAt,
            user.CreatedAt
        });
    }
}
