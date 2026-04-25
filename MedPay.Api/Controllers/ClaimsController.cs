using MedPay.Api.Dtos;
using MedPay.Core.Entities;
using MedPay.Core.Enums;
using MedPay.Core.Services;
using MedPay.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly MedPayDbContext _db;
    private readonly IClaimValidationService _validator;
    private readonly IAdjudicationService _adjudicator;

    public ClaimsController(MedPayDbContext db, IClaimValidationService validator, IAdjudicationService adjudicator)
    {
        _db = db;
        _validator = validator;
        _adjudicator = adjudicator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var claims = await _db.Claims.AsNoTracking()
            .Include(c => c.LineItems)
            .ToListAsync(ct);
        return Ok(claims.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var claim = await _db.Claims.AsNoTracking()
            .Include(c => c.LineItems)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (claim is null) return NotFound();
        return Ok(ToDto(claim));
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitClaimRequest req, CancellationToken ct)
    {
        if (req is null) return BadRequest("Request body is required.");

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = $"CLM-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}".Substring(0, 24),
            PatientId = req.PatientId,
            ProviderId = req.ProviderId,
            ServiceDate = req.ServiceDate,
            SubmittedAt = DateTime.UtcNow,
            Status = ClaimStatus.Submitted
        };
        foreach (var li in req.LineItems)
        {
            claim.LineItems.Add(new ClaimLineItem
            {
                Id = Guid.NewGuid(),
                CptCode = li.CptCode,
                Description = li.Description,
                Quantity = li.Quantity,
                ChargeAmount = li.ChargeAmount
            });
        }

        var validation = await _validator.ValidateAsync(claim, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationErrorResponse(validation.Errors));
        }

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, ToDto(claim));
    }

    [HttpPost("{id:guid}/adjudicate")]
    public async Task<IActionResult> Adjudicate(Guid id, CancellationToken ct)
    {
        var claim = await _db.Claims
            .AsNoTracking()
            .Include(c => c.LineItems)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (claim is null) return NotFound();

        var existing = await _db.Adjudications.AsNoTracking().AnyAsync(a => a.ClaimId == id, ct);
        if (existing) return Conflict("Claim has already been adjudicated.");

        var adj = await _adjudicator.AdjudicateAsync(claim, ct);

        var newStatus = adj.Decision == AdjudicationDecision.Deny ? ClaimStatus.Denied
            : adj.Decision == AdjudicationDecision.Approve ? ClaimStatus.Approved
            : ClaimStatus.PartiallyApproved;

        _db.Adjudications.Add(adj);
        await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Claims\" SET \"Status\" = {(int)newStatus} WHERE \"Id\" = {id}", ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new AdjudicationDto(adj.Id, adj.ClaimId, adj.Decision, adj.AdjudicatedAt, adj.PayerResponsibility, adj.PatientResponsibility, adj.AdjustedAmount, adj.DenialReason));
    }

    [HttpGet("{id:guid}/adjudication")]
    public async Task<IActionResult> GetAdjudication(Guid id, CancellationToken ct)
    {
        var adj = await _db.Adjudications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ClaimId == id, ct);
        if (adj is null) return NotFound("Claim has not been adjudicated yet.");

        return Ok(new AdjudicationDto(
            adj.Id,
            adj.ClaimId,
            adj.Decision,
            adj.AdjudicatedAt,
            adj.PayerResponsibility,
            adj.PatientResponsibility,
            adj.AdjustedAmount,
            adj.DenialReason
        ));
    }

    private static ClaimDto ToDto(Claim c)
    {
        var lines = c.LineItems.Select(li => new ClaimLineItemDto(li.CptCode, li.Description, li.Quantity, li.ChargeAmount)).ToList();
        var total = lines.Sum(li => li.ChargeAmount);
        return new ClaimDto(c.Id, c.ClaimNumber, c.PatientId, c.ProviderId, c.ServiceDate, c.SubmittedAt, c.Status, lines, total);
    }
}