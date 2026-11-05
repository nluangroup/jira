using System.Globalization;
using Lunch.Api.Data;
using Lunch.Api.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lunch.Api.MyMonth;

/// <summary>Endpoints for the current employee's lunch days and beverages.</summary>
public static class MyMonthEndpoints
{
    private const string DateFormat = "yyyy-MM-dd";
    private const int MaxBeverages = 999;
    private static readonly TimeZoneInfo Copenhagen = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    /// <summary>One month of the current employee's registrations.</summary>
    public sealed record MonthResponse(string Month, IReadOnlyList<string> LunchDays, int Beverages);

    /// <summary>Request body for setting the beverage count.</summary>
    public sealed record BeveragesRequest(int Count);

    /// <summary>Maps the <c>/api/me/months</c> and <c>/api/me/lunch-days</c> routes.</summary>
    public static void MapMyMonthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me/months/{month}", GetMonthAsync);
        app.MapPut("/api/me/lunch-days/{date}", TickAsync);
        app.MapDelete("/api/me/lunch-days/{date}", UntickAsync);
        app.MapPut("/api/me/months/{month}/beverages", SetBeveragesAsync);
    }

    private static async Task<IResult> GetMonthAsync(string month, HttpContext context, LunchDbContext db, CancellationToken ct)
    {
        if (!TryParseMonth(month, out var first))
        {
            return BadRequest("Month must be yyyy-MM.");
        }

        var employeeId = context.GetCurrentEmployee().Id;
        var next = first.AddMonths(1);
        var days = await db.LunchDays.AsNoTracking()
            .Where(d => d.EmployeeId == employeeId && d.Date >= first && d.Date < next)
            .OrderBy(d => d.Date)
            .Select(d => d.Date)
            .ToListAsync(ct);
        var beverages = await db.MonthBeverages.AsNoTracking()
            .Where(b => b.EmployeeId == employeeId && b.Month == month)
            .Select(b => b.Count)
            .SingleOrDefaultAsync(ct);

        return Results.Ok(new MonthResponse(month, days.ConvertAll(d => d.ToString(DateFormat, CultureInfo.InvariantCulture)), beverages));
    }

    private static async Task<IResult> TickAsync(string date, HttpContext context, LunchDbContext db, TimeProvider time, CancellationToken ct)
    {
        if (!TryParseDate(date, out var day))
        {
            return BadRequest("Date must be yyyy-MM-dd.");
        }

        if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return BadRequest("Lunch cannot be registered on weekends.");
        }

        if (day > Today(time))
        {
            return BadRequest("Lunch cannot be registered for future days.");
        }

        // An upsert, so two quick ticks of the same day can't race into a unique-index 500.
        var employeeId = context.GetCurrentEmployee().Id;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO LunchDays (EmployeeId, Date) VALUES ({employeeId}, {day}) ON CONFLICT (EmployeeId, Date) DO NOTHING", ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UntickAsync(string date, HttpContext context, LunchDbContext db, CancellationToken ct)
    {
        if (!TryParseDate(date, out var day))
        {
            return BadRequest("Date must be yyyy-MM-dd.");
        }

        var employeeId = context.GetCurrentEmployee().Id;
        await db.LunchDays.Where(d => d.EmployeeId == employeeId && d.Date == day).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetBeveragesAsync(string month, BeveragesRequest request, HttpContext context, LunchDbContext db, TimeProvider time, CancellationToken ct)
    {
        if (!TryParseMonth(month, out var first))
        {
            return BadRequest("Month must be yyyy-MM.");
        }

        if (request.Count is < 0 or > MaxBeverages)
        {
            return BadRequest($"Count must be between 0 and {MaxBeverages}.");
        }

        var today = Today(time);
        if (first > new DateOnly(today.Year, today.Month, 1))
        {
            return BadRequest("Beverages cannot be registered for a future month.");
        }

        var employeeId = context.GetCurrentEmployee().Id;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO MonthBeverages (EmployeeId, Month, Count) VALUES ({employeeId}, {month}, {request.Count}) ON CONFLICT (EmployeeId, Month) DO UPDATE SET Count = excluded.Count", ct);
        return Results.NoContent();
    }

    private static DateOnly Today(TimeProvider time) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), Copenhagen).DateTime);

    private static bool TryParseMonth(string value, out DateOnly firstOfMonth) =>
        DateOnly.TryParseExact($"{value}-01", DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out firstOfMonth);

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static IResult BadRequest(string detail) =>
        Results.Problem(title: "Invalid request", detail: detail, statusCode: StatusCodes.Status400BadRequest);
}
