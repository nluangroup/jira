using Microsoft.EntityFrameworkCore;

namespace Lunch.Api.Data;

/// <summary>Creates the schema and seeds employees from configuration.</summary>
public static class EmployeeSeeder
{
    /// <summary>Ensures the schema exists and inserts configured employees that are not yet stored.</summary>
    public static async Task SeedAsync(LunchDbContext db, IConfiguration configuration, CancellationToken cancellationToken)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var configured = configuration.GetSection("Employees").Get<List<Employee>>() ?? [];
        var existing = await db.Employees.Select(e => e.Id).ToListAsync(cancellationToken);
        db.Employees.AddRange(configured.Where(e => !existing.Contains(e.Id)));
        await db.SaveChangesAsync(cancellationToken);
    }
}
