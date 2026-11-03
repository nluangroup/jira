using Lunch.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Lunch.Api.Identity;

/// <summary>Endpoints for listing employees and reading the current one.</summary>
public static class IdentityEndpoints
{
    /// <summary>An employee as returned by the API.</summary>
    public sealed record EmployeeResponse(string Id, string Name, bool IsOfficeManager)
    {
        /// <summary>Maps a stored employee.</summary>
        public static EmployeeResponse From(Employee e) => new(e.Id, e.Name, e.IsOfficeManager);
    }

    /// <summary>Maps <c>GET /api/employees</c> and <c>GET /api/me</c>.</summary>
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/employees", async (LunchDbContext db, CancellationToken ct) =>
            await db.Employees.AsNoTracking().OrderBy(e => e.Name).Select(e => new EmployeeResponse(e.Id, e.Name, e.IsOfficeManager)).ToListAsync(ct));

        app.MapGet("/api/me", (HttpContext context) => EmployeeResponse.From(context.GetCurrentEmployee()));
    }
}
