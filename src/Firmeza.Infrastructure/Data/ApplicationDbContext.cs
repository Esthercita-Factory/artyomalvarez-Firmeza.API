using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Sku).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Description).HasMaxLength(800);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.UnitOfMeasure).HasMaxLength(32);
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.DocumentNumber).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(80);
            e.Property(x => x.LastName).HasMaxLength(80);
            e.Property(x => x.DocumentNumber).HasMaxLength(24);
            e.Property(x => x.DocumentType).HasMaxLength(32);
            e.Property(x => x.Email).HasMaxLength(254);
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.HasIndex(x => x.ExternalReference).IsUnique();
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.ExternalReference).HasMaxLength(40);
            e.Property(x => x.Subtotal).HasPrecision(14, 2);
            e.Property(x => x.TaxRate).HasPrecision(5, 4);
            e.Property(x => x.TaxAmount).HasPrecision(14, 2);
            e.Property(x => x.Total).HasPrecision(14, 2);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleDetail>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
            e.Property(x => x.LineTotal).HasPrecision(14, 2);
            e.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
