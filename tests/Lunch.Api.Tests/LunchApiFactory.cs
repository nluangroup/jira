using Lunch.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lunch.Api.Tests;

/// <summary>Hosts the API against a temporary SQLite file that lives as long as the factory.</summary>
/// <remarks>
/// Each request opens its own connection, as in production. A single shared connection is not
/// thread-safe, so concurrent requests through it fail.
/// </remarks>
public sealed class LunchApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"lunch-tests-{Guid.NewGuid():N}.db");

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LunchDbContext>>();
            services.AddDbContext<LunchDbContext>(options => options.UseSqlite($"Data Source={databasePath};Pooling=False"));
        });

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            File.Delete(databasePath);
        }
    }
}
