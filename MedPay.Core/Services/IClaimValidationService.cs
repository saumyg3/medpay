using MedPay.Core.Entities;

namespace MedPay.Core.Services;

public interface IClaimValidationService
{
    Task<Validation.ValidationResult> ValidateAsync(Claim claim, CancellationToken cancellationToken = default);
}
