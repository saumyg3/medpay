using MedPay.Core.Services;
using MedPay.Infrastructure.Data;
using MedPay.Infrastructure.Seed;
using MedPay.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Support both Npgsql-format connection strings and Render/Heroku URL format
var rawConn = builder.Configuration.GetConnectionString("MedPayDb") ?? Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(rawConn) && rawConn.StartsWith("postgres"))
{
    var uri = new Uri(rawConn);
    var userInfo = uri.UserInfo.Split(':');
    var port = uri.Port > 0 ? uri.Port : 5432;
    var dbName = uri.AbsolutePath.TrimStart('/');
    rawConn = $"Host={uri.Host};Port={port};Database={dbName};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<MedPayDbContext>(options =>
    options.UseNpgsql(rawConn));

builder.Services.AddScoped<IClaimValidationService, ClaimValidationService>();
builder.Services.AddScoped<IAdjudicationService, AdjudicationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedPayDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
