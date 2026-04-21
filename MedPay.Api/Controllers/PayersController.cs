using MedPay.Api.Dtos;
using MedPay.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayersController : ControllerBase
{
    private readonly MedPayDbContext _db;
    public PayersController(MedPayDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.Payers.AsNoTracking()
            .Select(p => new PayerDto(p.Id, p.Name, p.PayerCode))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var p = await _db.Payers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        return Ok(new PayerDto(p.Id, p.Name, p.PayerCode));
    }
}