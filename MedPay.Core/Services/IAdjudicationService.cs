using MedPay.Core.Entities;

namespace MedPay.Core.Services;

public interface IAdjudicationService
{
    Task<Adjudication> AdjudicateAsync(Claim claim, CancellationToken cancellationToken = default);
}
