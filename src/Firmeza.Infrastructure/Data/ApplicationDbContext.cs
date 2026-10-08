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
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Rental> Rentals => Set<Rental>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Sku).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Brand).HasMaxLength(80);
            e.Property(x => x.Description).HasMaxLength(800);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.UnitOfMeasure).HasMaxLength(32);
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentNumber).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(80);
            e.Property(x => x.LastName).HasMaxLength(80);
            e.Property(x => x.DocumentNumber).HasMaxLength(24);
            e.Property(x => x.DocumentType).HasMaxLength(32);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Phone).HasMaxLength(32);
            e.Property(x => x.Address).HasMaxLength(200);
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Plate).IsUnique();
            e.Property(x => x.Plate).HasMaxLength(20);
            e.Property(x => x.Brand).HasMaxLength(60);
            e.Property(x => x.Model).HasMaxLength(60);
            e.Property(x => x.LoadCapacity).HasMaxLength(50);
            e.Property(x => x.DailyRate).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Rental>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DailyRate).HasPrecision(12, 2);
            e.Property(x => x.Subtotal).HasPrecision(14, 2);
            e.Property(x => x.TaxRate).HasPrecision(5, 4);
            e.Property(x => x.TaxAmount).HasPrecision(14, 2);
            e.Property(x => x.Total).HasPrecision(14, 2);
            e.Property(x => x.Notes).HasMaxLength(500);

            e.HasOne(x => x.Customer)
                .WithMany(c => c.Rentals)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Vehicle)
                .WithMany(v => v.Rentals)
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ExternalReference).IsUnique();
            e.Property(x => x.Status).HasMaxLength(24);
            e.Property(x => x.ExternalReference).HasMaxLength(40);
            e.Property(x => x.Subtotal).HasPrecision(14, 2);
            e.Property(x => x.TaxRate).HasPrecision(5, 4);
            e.Property(x => x.TaxAmount).HasPrecision(14, 2);
            e.Property(x => x.Total).HasPrecision(14, 2);

            e.HasOne(x => x.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleDetail>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
            e.Property(x => x.LineTotal).HasPrecision(14, 2);

            e.HasOne(x => x.Sale)
                .WithMany(s => s.Details)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
