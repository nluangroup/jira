using Microsoft.EntityFrameworkCore;

namespace Lunch.Api.Data;

/// <summary>Database context for employees and their lunch registrations.</summary>
public sealed class LunchDbContext(DbContextOptions<LunchDbContext> options) : DbContext(options)
{
    /// <summary>All employees.</summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>Registered lunch days.</summary>
    public DbSet<LunchDay> LunchDays => Set<LunchDay>();

    /// <summary>Registered beverage counts per month.</summary>
    public DbSet<MonthBeverages> MonthBeverages => Set<MonthBeverages>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().HasKey(e => e.Id);
        modelBuilder.Entity<LunchDay>().HasIndex(d => new { d.EmployeeId, d.Date }).IsUnique();
        modelBuilder.Entity<MonthBeverages>().HasIndex(b => new { b.EmployeeId, b.Month }).IsUnique();
    }
}
