namespace MedPay.Core.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public required string MemberId { get; set; }

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
