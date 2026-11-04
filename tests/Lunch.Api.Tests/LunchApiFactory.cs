using Lunch.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lunch.Api.Tests;

/// <summary>Hosts the API against an in-memory SQLite database that lives as long as the factory.</summary>
public sealed class LunchApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    /// <summary>Opens the shared in-memory connection.</summary>
    public LunchApiFactory() => connection.Open();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LunchDbContext>>();
            services.AddDbContext<LunchDbContext>(options => options.UseSqlite(connection));
        });

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
        }
    }
}
