using MedPay.Api.Dtos;
using MedPay.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly MedPayDbContext _db;
    public PatientsController(MedPayDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var patients = await _db.Patients
            .AsNoTracking()
            .Select(p => new PatientDto(p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.MemberId))
            .ToListAsync(ct);
        return Ok(patients);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var p = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        return Ok(new PatientDto(p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.MemberId));
    }
}