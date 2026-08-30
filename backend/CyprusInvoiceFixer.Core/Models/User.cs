using System.ComponentModel.DataAnnotations;

namespace CyprusInvoiceFixer.Core.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FullName { get; set; }

    public UserPlan Plan { get; set; } = UserPlan.Free;

    public int MonthlyUsageCount { get; set; } = 0;

    public DateTime UsageResetAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public enum UserPlan
{
    Free = 0,
    Paid = 1
}
