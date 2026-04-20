namespace MedPay.Core.Entities;

public class Policy
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid PayerId { get; set; }
    public Payer Payer { get; set; } = null!;

    public required string PolicyNumber { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? TerminationDate { get; set; }

    public decimal AnnualDeductible { get; set; }
    public decimal DeductibleMetYtd { get; set; }
    public decimal OutOfPocketMaximum { get; set; }
    public decimal OutOfPocketMetYtd { get; set; }
    public decimal CoinsuranceRate { get; set; }
    public decimal CopayAmount { get; set; }

    public bool IsActiveOn(DateOnly date)
    {
        return date >= EffectiveDate && (TerminationDate == null || date <= TerminationDate);
    }
}
