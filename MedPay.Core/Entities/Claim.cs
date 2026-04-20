using MedPay.Core.Enums;

namespace MedPay.Core.Entities;

public class Claim
{
    public Guid Id { get; set; }
    public required string ClaimNumber { get; set; }

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public DateOnly ServiceDate { get; set; }
    public DateTime SubmittedAt { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;

    public ICollection<ClaimLineItem> LineItems { get; set; } = new List<ClaimLineItem>();
    public Adjudication? Adjudication { get; set; }

    public decimal TotalChargeAmount => LineItems.Sum(li => li.ChargeAmount);
}
