namespace MedPay.Core.Entities;

public class ClaimLineItem
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;

    public required string CptCode { get; set; }
    public required string Description { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal ChargeAmount { get; set; }
}
