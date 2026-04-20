using FluentAssertions;
using MedPay.Core.Entities;
using MedPay.Core.Enums;
using MedPay.Infrastructure.Data;
using MedPay.Infrastructure.Services;
using MedPay.Tests.Helpers;

namespace MedPay.Tests.Services;

public class AdjudicationServiceTests
{
    private static (MedPayDbContext db, Patient patient, Provider provider, Policy policy) Setup(
        decimal deductible = 1000m,
        decimal deductibleMet = 0m,
        decimal oopMax = 5000m,
        decimal oopMet = 0m,
        decimal coinsurance = 0.20m,
        decimal copay = 25m,
        DateOnly? effective = null,
        DateOnly? termination = null)
    {
        var db = TestDbFactory.Create();
        var payer = new Payer { Id = Guid.NewGuid(), Name = "Test Payer", PayerCode = "TEST" };
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "T", LastName = "P", DateOfBirth = new DateOnly(1990, 1, 1), MemberId = "MBR-TEST" };
        var provider = new Provider { Id = Guid.NewGuid(), Name = "T", NpiNumber = "9999999999", TaxId = "00-0", Specialty = "G" };
        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            PayerId = payer.Id,
            PolicyNumber = "POL-T-001",
            EffectiveDate = effective ?? new DateOnly(2026, 1, 1),
            TerminationDate = termination,
            AnnualDeductible = deductible,
            DeductibleMetYtd = deductibleMet,
            OutOfPocketMaximum = oopMax,
            OutOfPocketMetYtd = oopMet,
            CoinsuranceRate = coinsurance,
            CopayAmount = copay
        };
        db.Payers.Add(payer);
        db.Patients.Add(patient);
        db.Providers.Add(provider);
        db.Policies.Add(policy);
        db.SaveChanges();
        return (db, patient, provider, policy);
    }

    private static Claim BuildClaim(Patient patient, Provider provider, decimal charge, DateOnly? serviceDate = null)
    {
        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = $"CLM-{Guid.NewGuid():N}".Substring(0, 16),
            PatientId = patient.Id,
            ProviderId = provider.Id,
            ServiceDate = serviceDate ?? new DateOnly(2026, 6, 1),
            SubmittedAt = DateTime.UtcNow
        };
        claim.LineItems.Add(new ClaimLineItem
        {
            Id = Guid.NewGuid(),
            CptCode = "99213",
            Description = "Test",
            Quantity = 1,
            ChargeAmount = charge
        });
        return claim;
    }

    [Fact]
    public async Task Copya_plus_full_deductible_plus_coinsurance_splits_correctly()
    {
        var (db, patient, provider, policy) = Setup(deductible: 1000m, deductibleMet: 500m, coinsurance: 0.20m, copay: 25m);
        var service = new AdjudicationService(db);
        var claim = BuildClaim(patient, provider, 800m);
        var adj = await service.AdjudicateAsync(claim);
        // $25 copay, $500 deductible, remaining $275 * 20% = $55 patient. Payer: $220
        adj.PatientResponsibility.Should().Be(580m);
        adj.PayerResponsibility.Should().Be(220m);
        adj.Decision.Should().Be(AdjudicationDecision.PartialApprove);
    }

    [Fact]
    public async Task Deductible_already_met_patient_only_owes_copay_and_coinsurance()
    {
        var (db, patient, provider, _) = Setup(deductible: 1000m, deductibleMet: 1000m, coinsurance: 0.20m, copay: 25m);
        var service = new AdjudicationService(db);
        var claim = BuildClaim(patient, provider, 525m);
        var adj = await service.AdjudicateAsync(claim);
        // $25 copay + (500 * 20%) = $125 patient, payer $400
        adj.PatientResponsibility.Should().Be(125m);
        adj.PayerResponsibility.Should().Be(400m);
    }

    [Fact]
    public async Task Small_claim_below_copay_patient_pays_all()
    {
        var (db, patient, provider, _) = Setup(copay: 25m);
        var service = new AdjudicationService(db);
        var claim = BuildClaim(patient, provider, 10m);
        var adj = await service.AdjudicateAsync(claim);
        adj.PatientResponsibility.Should().Be(10m);
        adj.PayerResponsibility.Should().Be(0m);
    }

    [Fact]
    public async Task Out_of_pocket_max_caps_patient_responsibility()
    {
        var (db, patient, provider, _) = Setup(deductible: 1000m, deductibleMet: 0m, oopMax: 1000m, oopMet: 900m, copay: 25m);
        var service = new AdjudicationService(db);
        var claim = BuildClaim(patient, provider, 500m);
        var adj = await service.AdjudicateAsync(claim);
        // Without oop cap: patient = 25 copay + 475 deductible = 500. Likely capped at 100 (max 1000 - met 900).
        adj.PatientResponsibility.Should().Be(100m);
        adj.PayerResponsibility.Should().Be(400m);
    }

    [Fact]
    public async Task Zero_coinsurance_payer_pays_all_after_deductible()
    {
        var (db, patient, provider, _) = Setup(deductible: 500m, deductibleMet: 500m, coinsurance: 0m, copay: 0m);
        var service = new AdjudicationService(db);
        var claim = BuildClaim(patient, provider, 1000m);
        var adj = await service.AdjudicateAsync(claim);
        adj.PatientResponsibility.Should().Be(0m);
        adj.PayerResponsibility.Should().Be(1000m);
    }

    [Fact]
    public async Task No_active_policy_returns_denial()
    {
        var (db, patient, provider, _) = Setup(effective: new DateOnly(2024, 1, 1), termination: new DateOnly(2024, 12, 31));
        var service = new AdjudicationService(db);
        var claim = BuildClaim(patient, provider, 500m, new DateOnly(2026, 6, 1));
        var adj = await service.AdjudicateAsync(claim);
        adj.Decision.Should().Be(AdjudicationDecision.Deny);
        adj.PatientResponsibility.Should().Be(0m);
        adj.PayerResponsibility.Should().Be(0m);
    }

    [Fact]
    public async Task Partial_deductible_met_remainder_applied_correctly()
    {
        var (db, patient, provider, _) = Setup(deductible: 1000m, deductibleMet: 800m, coinsurance: 0.20m, copay: 0m);
        var service = new AdjudicationService(db);
        var claim = BuildClaim(patient, provider, 300m);
        var adj = await service.AdjudicateAsync(claim);
        // 200 deductible remaining + (100 * 20% = 20) = 220 patient, payer 80
        adj.PatientResponsibility.Should().Be(220m);
        adj.PayerResponsibility.Should().Be(80m);
    }

    [Fact]
    public async Task Policy_trackers_are_updated_after_adjudication()
    {
        var (db, patient, provider, policy) = Setup(deductible: 1000m, deductibleMet: 500m, copay: 25m);
        var service = new AdjudicationService(db);
        var claim = BuildClaim(patient, provider, 800m);
        await service.AdjudicateAsync(claim);
        policy.DeductibleMetYtd.Should().Be(1000m);
        policy.OutOfPocketMetYtd.Should().BeGreaterThan(0m);
    }
}
