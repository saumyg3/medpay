using MedPay.Core.Entities;
using MedPay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(MedPayDbContext db)
    {
        if (await db.Payers.AnyAsync())
        {
            return;
        }

        // Payers
        var aetna = new Payer
        {
            Id = Guid.NewGuid(),
            Name = "Aetna Health Inc.",
            PayerCode = "AETNA"
        };
        var bcbs = new Payer
        {
            Id = Guid.NewGuid(),
            Name = "Blue Cross Blue Shield of California",
            PayerCode = "BCBS_CA"
        };
        db.Payers.AddRange(aetna, bcbs);

        // Providers
        var drChen = new Provider
        {
            Id = Guid.NewGuid(),
            Name = "Dr. Linda Chen, MD",
            NpiNumber = "1234567890",
            TaxId = "12-3456789",
            Specialty = "Internal Medicine"
        };
        var drPatel = new Provider
        {
            Id = Guid.NewGuid(),
            Name = "Dr. Arjun Patel, DO",
            NpiNumber = "2345678901",
            TaxId = "23-4567890",
            Specialty = "Cardiology"
        };
        var uciHealth = new Provider
        {
            Id = Guid.NewGuid(),
            Name = "UCI Medical Center",
            NpiNumber = "3456789012",
            TaxId = "34-5678901",
            Specialty = "General Hospital"
        };
        db.Providers.AddRange(drChen, drPatel, uciHealth);

        // Patients
        var alice = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Johnson",
            DateOfBirth = new DateOnly(1985, 3, 14),
            MemberId = "MBR-001"
        };
        var bob = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Martinez",
            DateOfBirth = new DateOnly(1972, 11, 2),
            MemberId = "MBR-002"
        };
        var carol = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "Carol",
            LastName = "Nguyen",
            DateOfBirth = new DateOnly(1990, 7, 22),
            MemberId = "MBR-003"
        };
        var david = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "David",
            LastName = "OConnor",
            DateOfBirth = new DateOnly(1968, 1, 30),
            MemberId = "MBR-004"
        };
        db.Patients.AddRange(alice, bob, carol, david);

        // Policies
        // Alice: active Aetna policy, has not hit deductible
        db.Policies.Add(new Policy
        {
            Id = Guid.NewGuid(),
            PatientId = alice.Id,
            PayerId = aetna.Id,
            PolicyNumber = "POL-AETNA-001",
            EffectiveDate = new DateOnly(2026, 1, 1),
            TerminationDate = null,
            AnnualDeductible = 1500.00m,
            DeductibleMetYtd = 400.00m,
            OutOfPocketMaximum = 8000.00m,
            OutOfPocketMetYtd = 400.00m,
            CoinsuranceRate = 0.20m,
            CopayAmount = 30.00m
        });

        // Bob: active BCBS policy, deductible fully met
        db.Policies.Add(new Policy
        {
            Id = Guid.NewGuid(),
            PatientId = bob.Id,
            PayerId = bcbs.Id,
            PolicyNumber = "POL-BCBS-001",
            EffectiveDate = new DateOnly(2026, 1, 1),
            TerminationDate = null,
            AnnualDeductible = 2000.00m,
            DeductibleMetYtd = 2000.00m,
            OutOfPocketMaximum = 6000.00m,
            OutOfPocketMetYtd = 2500.00m,
            CoinsuranceRate = 0.10m,
            CopayAmount = 25.00m
        });

        // Carol: active Aetna policy, brand new (nothing met)
        db.Policies.Add(new Policy
        {
            Id = Guid.NewGuid(),
            PatientId = carol.Id,
            PayerId = aetna.Id,
            PolicyNumber = "POL-AETNA-002",
            EffectiveDate = new DateOnly(2026, 3, 1),
            TerminationDate = null,
            AnnualDeductible = 1000.00m,
            DeductibleMetYtd = 0.00m,
            OutOfPocketMaximum = 5000.00m,
            OutOfPocketMetYtd = 0.00m,
            CoinsuranceRate = 0.20m,
            CopayAmount = 40.00m
        });

        // David: expired policy (terminated 2025)
        db.Policies.Add(new Policy
        {
            Id = Guid.NewGuid(),
            PatientId = david.Id,
            PayerId = bcbs.Id,
            PolicyNumber = "POL-BCBS-002",
            EffectiveDate = new DateOnly(2024, 1, 1),
            TerminationDate = new DateOnly(2025, 12, 31),
            AnnualDeductible = 1500.00m,
            DeductibleMetYtd = 0.00m,
            OutOfPocketMaximum = 7000.00m,
            OutOfPocketMetYtd = 0.00m,
            CoinsuranceRate = 0.20m,
            CopayAmount = 35.00m
        });

        await db.SaveChangesAsync();
    }
}
