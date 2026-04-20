using MedPay.Core.Services;
using MedPay.Infrastructure.Data;
using MedPay.Infrastructure.Seed;
using MedPay.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MedPayDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MedPayDb")));

builder.Services.AddScoped<IClaimValidationService, ClaimValidationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedPayDbContext>();
    await DatabaseSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
