namespace MedPay.Core.Entities;

public class LedgerEntry
{
    public Guid Id { get; set; }
    public Guid AdjudicationId { get; set; }
    public Adjudication Adjudication { get; set; } = null!;

    public required string Account { get; set; }
    public decimal Amount { get; set; }
    public DateTime PostedAt { get; set; }
    public required string Description { get; set; }
}
