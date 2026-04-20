using System.Text.RegularExpressions;
using MedPay.Core.Entities;
using MedPay.Core.Services;
using MedPay.Core.Validation;
using MedPay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Infrastructure.Services;

public class ClaimValidationService : IClaimValidationService
{
    private static readonly Regex CptCodePattern = new(@"^\d{5}$", RegexOptions.Compiled);

    private readonly MedPayDbContext _db;

    public ClaimValidationService(MedPayDbContext db)
    {
        _db = db;
    }

    public async Task<ValidationResult> ValidateAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        // Rule 1: Patient must exist
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == claim.PatientId, cancellationToken);
        if (patient is null)
        {
            result.AddError($"Patient {claim.PatientId} not found.");
            return result;
        }

        // Rule 2: Provider must exist
        var providerExists = await _db.Providers
            .AsNoTracking()
            .AnyAsync(p => p.Id == claim.ProviderId, cancellationToken);
        if (!providerExists)
        {
            result.AddError($"Provider {claim.ProviderId} not found.");
        }

        // Rule 3: Patient must have active policy on the service date
        var activePolicy = await _db.Policies
            .AsNoTracking()
            .Where(p => p.PatientId == claim.PatientId)
            .Where(p => claim.ServiceDate >= p.EffectiveDate)
            .Where(p => p.TerminationDate == null || claim.ServiceDate <= p.TerminationDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (activePolicy is null)
        {
            result.AddError($"No active policy for patient on service date {claim.ServiceDate:yyyy-MM-dd}.");
        }

        // Rule 4: Claim must have at least one line item
        if (claim.LineItems is null || !claim.LineItems.Any())
        {
            result.AddError("Claim must contain at least one line item.");
        }
        else
        {
            // Rule 5: CPT codes must be 5 digits
            foreach (var li in claim.LineItems)
            {
                if (string.IsNullOrWhiteSpace(li.CptCode) || !CptCodePattern.IsMatch(li.CptCode))
                {
                    result.AddError($"Invalid CPT code format: '{li.CptCode}' (expected 5 digits).");
                }
                if (li.ChargeAmount <= 0)
                {
                    result.AddError($"Line item for CPT {li.CptCode} has non-positive charge amount.");
                }
            }
        }

        // Rule 6: Duplicate claim detection
        // Same patient, same provider, same service date, same set of CPT codes
        if (claim.LineItems is not null && claim.LineItems.Any())
        {
            var cptCodes = claim.LineItems.Select(li => li.CptCode).OrderBy(c => c).ToList();

            var potentialDuplicates = await _db.Claims
                .AsNoTracking()
                .Include(c => c.LineItems)
                .Where(c => c.Id != claim.Id)
                .Where(c => c.PatientId == claim.PatientId)
                .Where(c => c.ProviderId == claim.ProviderId)
                .Where(c => c.ServiceDate == claim.ServiceDate)
                .ToListAsync(cancellationToken);

            foreach (var existing in potentialDuplicates)
            {
                var existingCodes = existing.LineItems.Select(li => li.CptCode).OrderBy(c => c).ToList();
                if (existingCodes.SequenceEqual(cptCodes))
                {
                    result.AddError($"Duplicate claim detected: existing claim {existing.ClaimNumber} matches patient, provider, service date, and line items.");
                    break;
                }
            }
        }

        return result;
    }
}
