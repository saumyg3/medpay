using MedPay.Core.Entities;
using MedPay.Core.Enums;
using MedPay.Core.Services;
using MedPay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Infrastructure.Services;

public class AdjudicationService : IAdjudicationService
{
    private readonly MedPayDbContext _db;

    public AdjudicationService(MedPayDbContext db)
    {
        _db = db;
    }

    public async Task<Adjudication> AdjudicateAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        var policy = await _db.Policies
            .Where(p => p.PatientId == claim.PatientId)
            .Where(p => claim.ServiceDate >= p.EffectiveDate)
            .Where(p => p.TerminationDate == null || claim.ServiceDate <= p.TerminationDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            return new Adjudication
            {
                Id = Guid.NewGuid(),
                ClaimId = claim.Id,
                Decision = AdjudicationDecision.Deny,
                AdjudicatedAt = DateTime.UtcNow,
                PayerResponsibility = 0m,
                PatientResponsibility = 0m,
                AdjustedAmount = claim.TotalChargeAmount,
                DenialReason = "No active policy on service date."
            };
        }

        var totalCharge = claim.TotalChargeAmount;
        if (totalCharge <= 0m)
        {
            return new Adjudication
            {
                Id = Guid.NewGuid(),
                ClaimId = claim.Id,
                Decision = AdjudicationDecision.Deny,
                AdjudicatedAt = DateTime.UtcNow,
                PayerResponsibility = 0m,
                PatientResponsibility = 0m,
                AdjustedAmount = 0m,
                DenialReason = "Claim has no chargeable amount."
            };
        }

        var remaining = totalCharge;
        var patientOwes = 0m;
        var payerOwes = 0m;

        var copay = Math.Min(policy.CopayAmount, remaining);
        patientOwes += copay;
        remaining -= copay;

        var deductibleRemaining = Math.Max(0m, policy.AnnualDeductible - policy.DeductibleMetYtd);
        var deductibleApplied = Math.Min(deductibleRemaining, remaining);
        patientOwes += deductibleApplied;
        remaining -= deductibleApplied;

        var coinsurancePatientShare = decimal.Round(remaining * policy.CoinsuranceRate, 2, MidpointRounding.AwayFromZero);
        var coinsurancePayerShare = remaining - coinsurancePatientShare;
        patientOwes += coinsurancePatientShare;
        payerOwes += coinsurancePayerShare;

        var oopRemaining = Math.Max(0m, policy.OutOfPocketMaximum - policy.OutOfPocketMetYtd);
        if (patientOwes > oopRemaining)
        {
            var excess = patientOwes - oopRemaining;
            patientOwes = oopRemaining;
            payerOwes += excess;
        }

        policy.DeductibleMetYtd += deductibleApplied;
        policy.OutOfPocketMetYtd += patientOwes;

        var decision = payerOwes > 0m
            ? (patientOwes > 0m ? AdjudicationDecision.PartialApprove : AdjudicationDecision.Approve)
            : AdjudicationDecision.PartialApprove;

        return new Adjudication
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            Decision = decision,
            AdjudicatedAt = DateTime.UtcNow,
            PayerResponsibility = payerOwes,
            PatientResponsibility = patientOwes,
            AdjustedAmount = 0m
        };
    }
}
