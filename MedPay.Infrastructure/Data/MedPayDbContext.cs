using MedPay.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Infrastructure.Data;

public class MedPayDbContext : DbContext
{
    public MedPayDbContext(DbContextOptions<MedPayDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Payer> Payers => Set<Payer>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimLineItem> ClaimLineItems => Set<ClaimLineItem>();
    public DbSet<Adjudication> Adjudications => Set<Adjudication>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>(e =>
        {
            e.HasIndex(p => p.MemberId).IsUnique();
            e.Property(p => p.FirstName).HasMaxLength(100);
            e.Property(p => p.LastName).HasMaxLength(100);
            e.Property(p => p.MemberId).HasMaxLength(50);
        });

        modelBuilder.Entity<Provider>(e =>
        {
            e.HasIndex(p => p.NpiNumber).IsUnique();
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.NpiNumber).HasMaxLength(10);
            e.Property(p => p.TaxId).HasMaxLength(20);
            e.Property(p => p.Specialty).HasMaxLength(100);
        });

        modelBuilder.Entity<Payer>(e =>
        {
            e.HasIndex(p => p.PayerCode).IsUnique();
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.PayerCode).HasMaxLength(50);
        });

        modelBuilder.Entity<Policy>(e =>
        {
            e.HasIndex(p => p.PolicyNumber).IsUnique();
            e.Property(p => p.PolicyNumber).HasMaxLength(50);
            e.Property(p => p.AnnualDeductible).HasPrecision(10, 2);
            e.Property(p => p.DeductibleMetYtd).HasPrecision(10, 2);
            e.Property(p => p.OutOfPocketMaximum).HasPrecision(10, 2);
            e.Property(p => p.OutOfPocketMetYtd).HasPrecision(10, 2);
            e.Property(p => p.CoinsuranceRate).HasPrecision(5, 4);
            e.Property(p => p.CopayAmount).HasPrecision(10, 2);

            e.HasOne(p => p.Patient)
                .WithMany(pt => pt.Policies)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Payer)
                .WithMany(py => py.Policies)
                .HasForeignKey(p => p.PayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Claim>(e =>
        {
            e.HasIndex(c => c.ClaimNumber).IsUnique();
            e.Property(c => c.ClaimNumber).HasMaxLength(50);

            e.HasOne(c => c.Patient)
                .WithMany(p => p.Claims)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Provider)
                .WithMany(p => p.Claims)
                .HasForeignKey(c => c.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Ignore(c => c.TotalChargeAmount);
        });

        modelBuilder.Entity<ClaimLineItem>(e =>
        {
            e.Property(li => li.CptCode).HasMaxLength(10);
            e.Property(li => li.Description).HasMaxLength(500);
            e.Property(li => li.ChargeAmount).HasPrecision(10, 2);

            e.HasOne(li => li.Claim)
                .WithMany(c => c.LineItems)
                .HasForeignKey(li => li.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Adjudication>(e =>
        {
            e.Property(a => a.PayerResponsibility).HasPrecision(10, 2);
            e.Property(a => a.PatientResponsibility).HasPrecision(10, 2);
            e.Property(a => a.AdjustedAmount).HasPrecision(10, 2);
            e.Property(a => a.DenialReason).HasMaxLength(500);

            e.HasOne(a => a.Claim)
                .WithOne(c => c.Adjudication!)
                .HasForeignKey<Adjudication>(a => a.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LedgerEntry>(e =>
        {
            e.Property(l => l.Account).HasMaxLength(50);
            e.Property(l => l.Description).HasMaxLength(500);
            e.Property(l => l.Amount).HasPrecision(10, 2);

            e.HasOne(l => l.Adjudication)
                .WithMany(a => a.LedgerEntries)
                .HasForeignKey(l => l.AdjudicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
