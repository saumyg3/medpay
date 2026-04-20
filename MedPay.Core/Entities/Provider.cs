namespace MedPay.Core.Entities;

public class Provider
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string NpiNumber { get; set; }
    public required string TaxId { get; set; }
    public required string Specialty { get; set; }

    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
