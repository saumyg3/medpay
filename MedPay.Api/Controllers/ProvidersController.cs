using MedPay.Api.Dtos;
using MedPay.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly MedPayDbContext _db;
    public ProvidersController(MedPayDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.Providers.AsNoTracking()
            .Select(p => new ProviderDto(p.Id, p.Name, p.NpiNumber, p.Specialty))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var p = await _db.Providers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        return Ok(new ProviderDto(p.Id, p.Name, p.NpiNumber, p.Specialty));
    }
}