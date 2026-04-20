using FluentAssertions;
using MedPay.Core.Entities;
using MedPay.Infrastructure.Data;
using MedPay.Infrastructure.Services;
using MedPay.Tests.Helpers;

namespace MedPay.Tests.Services;

public class ClaimValidationServiceTests
{
    private static (MedPayDbContext db, Patient patient, Provider provider) Setup()
    {
        var db = TestDbFactory.Create();

        var payer = new Payer { Id = Guid.NewGuid(), Name = "Test Payer", PayerCode = "TEST" };
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Test", LastName = "Patient", DateOfBirth = new DateOnly(1990, 1, 1), MemberId = "MBR-TEST" };
        var provider = new Provider { Id = Guid.NewGuid(), Name = "Test Provider", NpiNumber = "9999999999", TaxId = "00-0000000", Specialty = "General" };
        var policy = new Policy { Id = Guid.NewGuid(), PatientId = patient.Id, PayerId = payer.Id, PolicyNumber = "POL-TEST-001", EffectiveDate = new DateOnly(2026, 1, 1), TerminationDate = null, AnnualDeductible = 1000, DeductibleMetYtd = 0, OutOfPocketMaximum = 5000, OutOfPocketMetYtd = 0, CoinsuranceRate = 0.20m, CopayAmount = 25 };

        db.Payers.Add(payer);
        db.Patients.Add(patient);
        db.Providers.Add(provider);
        db.Policies.Add(policy);
        db.SaveChanges();

        return (db, patient, provider);
    }

    private static Claim BuildClaim(Patient patient, Provider provider, DateOnly serviceDate, params (string cpt, decimal charge)[] lines)
    {
        var claim = new Claim { Id = Guid.NewGuid(), ClaimNumber = $"CLM-{Guid.NewGuid():N}".Substring(0, 16), PatientId = patient.Id, ProviderId = provider.Id, ServiceDate = serviceDate, SubmittedAt = DateTime.UtcNow };
        foreach (var line in lines)
        {
            claim.LineItems.Add(new ClaimLineItem { Id = Guid.NewGuid(), CptCode = line.cpt, Description = "Test service", Quantity = 1, ChargeAmount = line.charge });
        }
        return claim;
    }

    [Fact]
    public async Task Valid_claim_passes_validation()
    {
        var (db, patient, provider) = Setup();
        var service = new ClaimValidationService(db);
        var claim = BuildClaim(patient, provider, new DateOnly(2026, 6, 1), ("99213", 150m));
        var result = await service.ValidateAsync(claim);
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_patient_fails_validation()
    {
        var (db, _, provider) = Setup();
        var service = new ClaimValidationService(db);
        var claim = BuildClaim(new Patient { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", MemberId = "MBR-X" }, provider, new DateOnly(2026, 6, 1), ("99213", 150m));
        var result = await service.ValidateAsync(claim);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Patient") && e.Contains("not found"));
    }

    [Fact]
    public async Task Unknown_provider_fails_validation()
    {
        var (db, patient, _) = Setup();
        var service = new ClaimValidationService(db);
        var claim = BuildClaim(patient, new Provider { Id = Guid.NewGuid(), Name = "X", NpiNumber = "0000000000", TaxId = "00-0000000", Specialty = "X" }, new DateOnly(2026, 6, 1), ("99213", 150m));
        var result = await service.ValidateAsync(claim);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Provider") && e.Contains("not found"));
    }

    [Fact]
    public async Task Service_date_before_policy_effective_date_fails_validation()
    {
        var (db, patient, provider) = Setup();
        var service = new ClaimValidationService(db);
        var claim = BuildClaim(patient, provider, new DateOnly(2025, 6, 1), ("99213", 150m));
        var result = await service.ValidateAsync(claim);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("No active policy"));
    }

    [Fact]
    public async Task Claim_with_no_line_items_fails_validation()
    {
        var (db, patient, provider) = Setup();
        var service = new ClaimValidationService(db);
        var claim = BuildClaim(patient, provider, new DateOnly(2026, 6, 1));
        var result = await service.ValidateAsync(claim);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("at least one line item"));
    }

    [Fact]
    public async Task Invalid_cpt_code_format_fails_validation()
    {
        var (db, patient, provider) = Setup();
        var service = new ClaimValidationService(db);
        var claim = BuildClaim(patient, provider, new DateOnly(2026, 6, 1), ("ABC", 150m));
        var result = await service.ValidateAsync(claim);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid CPT code"));
    }

    [Fact]
    public async Task Non_positive_charge_amount_fails_validation()
    {
        var (db, patient, provider) = Setup();
        var service = new ClaimValidationService(db);
        var claim = BuildClaim(patient, provider, new DateOnly(2026, 6, 1), ("99213", 0m));
        var result = await service.ValidateAsync(claim);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("non-positive charge"));
    }

    [Fact]
    public async Task Duplicate_claim_fails_validation()
    {
        var (db, patient, provider) = Setup();
        var service = new ClaimValidationService(db);
        var original = BuildClaim(patient, provider, new DateOnly(2026, 6, 1), ("99213", 150m));
        db.Claims.Add(original);
        await db.SaveChangesAsync();
        var duplicate = BuildClaim(patient, provider, new DateOnly(2026, 6, 1), ("99213", 150m));
        var result = await service.ValidateAsync(duplicate);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Duplicate claim"));
    }

    [Fact]
    public async Task Multiple_errors_are_all_reported()
    {
        var (db, patient, _) = Setup();
        var service = new ClaimValidationService(db);
        var claim = BuildClaim(patient, new Provider { Id = Guid.NewGuid(), Name = "X", NpiNumber = "0", TaxId = "0", Specialty = "X" }, new DateOnly(2026, 6, 1), ("BAD", 0m));
        var result = await service.ValidateAsync(claim);
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterThan(1);
    }
}
