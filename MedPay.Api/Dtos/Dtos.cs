using MedPay.Core.Enums;

namespace MedPay.Api.Dtos;

public record PatientDto(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, string MemberId);

public record ProviderDto(Guid Id, string Name, string NpiNumber, string Specialty);

public record PayerDto(Guid Id, string Name, string PayerCode);

public record PolicyDto(
    Guid Id,
    string PolicyNumber,
    Guid PatientId,
    Guid PayerId,
    DateOnly EffectiveDate,
    DateOnly? TerminationDate,
    decimal AnnualDeductible,
    decimal DeductibleMetYtd,
    decimal OutOfPocketMaximum,
    decimal OutOfPocketMetYtd,
    decimal CoinsuranceRate,
    decimal CopayAmount);

public record ClaimLineItemDto(string CptCode, string Description, int Quantity, decimal ChargeAmount);

public record ClaimDto(
    Guid Id,
    string ClaimNumber,
    Guid PatientId,
    Guid ProviderId,
    DateOnly ServiceDate,
    DateTime SubmittedAt,
    ClaimStatus Status,
    IReadOnlyList<ClaimLineItemDto> LineItems,
    decimal TotalChargeAmount);

public record SubmitClaimRequest(
    Guid PatientId,
    Guid ProviderId,
    DateOnly ServiceDate,
    List<ClaimLineItemDto> LineItems);

public record AdjudicationDto(
    Guid Id,
    Guid ClaimId,
    AdjudicationDecision Decision,
    DateTime AdjudicatedAt,
    decimal PayerResponsibility,
    decimal PatientResponsibility,
    decimal AdjustedAmount,
    string? DenialReason);

public record ValidationErrorResponse(List<string> Errors);