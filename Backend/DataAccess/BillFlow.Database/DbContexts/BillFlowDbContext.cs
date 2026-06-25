using BillFlow.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Database.DbContexts;

public class BillFlowDbContext : DbContext
{
    public BillFlowDbContext(DbContextOptions<BillFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasQueryFilter(x => !x.IsDeleted);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.FullName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.Property(x => x.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(x => x.Role)
                .IsRequired();

            entity.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Clients)
                .WithOne(x => x.Owner)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Items)
                .WithOne(x => x.Owner)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Invoices)
                .WithOne(x => x.Owner)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token)
                .HasMaxLength(512)
                .IsRequired();

            entity.HasIndex(x => x.Token)
                .IsUnique();

            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasQueryFilter(x => !x.IsDeleted);

            entity.HasIndex(x => new { x.OwnerId, x.Email })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.Property(x => x.CompanyName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.ContactName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(x => x.Address)
                .HasMaxLength(500);

            entity.Property(x => x.Country)
                .HasMaxLength(100);

            entity.Property(x => x.TaxNumber)
                .HasMaxLength(50);

            entity.Property(x => x.Notes)
                .HasMaxLength(2000);

            entity.HasMany(x => x.Invoices)
                .WithOne(x => x.Client)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasQueryFilter(x => !x.IsDeleted);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(x => x.VatRate)
                .HasPrecision(5, 2);

            entity.Property(x => x.Category)
                .HasMaxLength(100);

            entity.Property(x => x.Unit)
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasQueryFilter(x => !x.IsDeleted);

            entity.HasIndex(x => new { x.OwnerId, x.InvoiceNumber })
                .IsUnique();

            entity.Property(x => x.InvoiceNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(x => x.TaxRate)
                .HasPrecision(5, 2);

            entity.Property(x => x.TaxAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Total)
                .HasPrecision(18, 2);

            entity.Property(x => x.Notes)
                .HasMaxLength(2000);

            entity.HasMany(x => x.LineItems)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Payments)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 4);

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.LineTotal)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Item)
                .WithMany(x => x.LineItems)
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new { x.OwnerId, x.InvoiceId, x.PaymentDate });

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Reference)
                .HasMaxLength(100);

            entity.Property(x => x.Notes)
                .HasMaxLength(500);

            entity.Property(x => x.Method)
                .IsRequired();

            entity.Property(x => x.Status)
                .IsRequired();
        });
    }
}
