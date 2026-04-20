namespace MedPay.Core.Entities;

public class Payer
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string PayerCode { get; set; }

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
