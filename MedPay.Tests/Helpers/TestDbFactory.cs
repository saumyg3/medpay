using MedPay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedPay.Tests.Helpers;

public static class TestDbFactory
{
    public static MedPayDbContext Create()
    {
        var options = new DbContextOptionsBuilder<MedPayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MedPayDbContext(options);
    }
}
