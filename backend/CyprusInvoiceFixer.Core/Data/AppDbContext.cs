using CyprusInvoiceFixer.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CyprusInvoiceFixer.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<ValidationIssue> ValidationIssues => Set<ValidationIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Plan).HasConversion<string>();
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Status).HasConversion<string>();
            e.Property(i => i.Subtotal).HasPrecision(18, 4);
            e.Property(i => i.VatRate).HasPrecision(5, 2);
            e.Property(i => i.VatAmount).HasPrecision(18, 4);
            e.Property(i => i.Total).HasPrecision(18, 4);
            e.HasOne(i => i.User).WithMany(u => u.Invoices)
                .HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.LineItems).WithOne(li => li.Invoice)
                .HasForeignKey(li => li.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.ValidationIssues).WithOne(vi => vi.Invoice)
                .HasForeignKey(vi => vi.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLineItem>(e =>
        {
            e.HasKey(li => li.Id);
            e.Property(li => li.Quantity).HasPrecision(18, 4);
            e.Property(li => li.UnitPrice).HasPrecision(18, 4);
            e.Ignore(li => li.LineTotal);
        });

        modelBuilder.Entity<ValidationIssue>(e =>
        {
            e.HasKey(vi => vi.Id);
            e.Property(vi => vi.Severity).HasConversion<string>();
        });
    }
}
