namespace MedPay.Core.Validation;

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; } = new();

    public void AddError(string error) => Errors.Add(error);

    public static ValidationResult Success() => new();
    public static ValidationResult Failure(params string[] errors)
    {
        var result = new ValidationResult();
        foreach (var e in errors) result.AddError(e);
        return result;
    }
}
