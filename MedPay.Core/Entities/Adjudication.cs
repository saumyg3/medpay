using MedPay.Core.Enums;

namespace MedPay.Core.Entities;

public class Adjudication
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;

    public AdjudicationDecision Decision { get; set; }
    public DateTime AdjudicatedAt { get; set; }

    public decimal PayerResponsibility { get; set; }
    public decimal PatientResponsibility { get; set; }
    public decimal AdjustedAmount { get; set; }

    public string? DenialReason { get; set; }

    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}
