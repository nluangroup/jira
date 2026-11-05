using System.Globalization;
using System.Text;
using Lunch.Api.Data;
using Lunch.Api.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lunch.Api.Reports;

/// <summary>Manager-only monthly overview of every employee's registrations.</summary>
public static class ReportEndpoints
{
    private const string CsvHeader = "Employee;Lunch days;Beverages";

    /// <summary>One employee's totals for the month.</summary>
    public sealed record ReportRow(string EmployeeId, string Name, int LunchDays, int Beverages);

    /// <summary>Column sums over all rows.</summary>
    public sealed record ReportTotal(int LunchDays, int Beverages);

    /// <summary>The monthly overview.</summary>
    public sealed record ReportResponse(string Month, IReadOnlyList<ReportRow> Rows, ReportTotal Total);

    /// <summary>Maps the <c>/api/reports</c> routes.</summary>
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/{month}", GetReportAsync);
        app.MapGet("/api/reports/{month}/csv", GetCsvAsync);
    }

    private static async Task<IResult> GetReportAsync(string month, HttpContext context, LunchDbContext db, CancellationToken ct)
    {
        if (Validate(month, context) is { } error)
        {
            return error;
        }

        return Results.Ok(await BuildAsync(month, db, ct));
    }

    private static async Task<IResult> GetCsvAsync(string month, HttpContext context, LunchDbContext db, CancellationToken ct)
    {
        if (Validate(month, context) is { } error)
        {
            return error;
        }

        var report = await BuildAsync(month, db, ct);
        var csv = new StringBuilder(CsvHeader).Append("\r\n");
        foreach (var row in report.Rows)
        {
            csv.Append(CsvField(row.Name)).Append(';').Append(row.LunchDays).Append(';').Append(row.Beverages).Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return Results.File(bytes, "text/csv; charset=utf-8", $"lunch-{month}.csv");
    }

    private static IResult? Validate(string month, HttpContext context)
    {
        if (!context.GetCurrentEmployee().IsOfficeManager)
        {
            return Results.Problem(title: "Forbidden", detail: "Only office managers can read reports.", statusCode: StatusCodes.Status403Forbidden);
        }

        return DateOnly.TryParseExact($"{month}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? null
            : Results.Problem(title: "Invalid request", detail: "Month must be yyyy-MM.", statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<ReportResponse> BuildAsync(string month, LunchDbContext db, CancellationToken ct)
    {
        var first = DateOnly.ParseExact($"{month}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var next = first.AddMonths(1);
        var employees = await db.Employees.AsNoTracking().Select(e => new { e.Id, e.Name }).ToListAsync(ct);
        var days = await db.LunchDays.AsNoTracking()
            .Where(d => d.Date >= first && d.Date < next)
            .GroupBy(d => d.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, ct);
        var beverages = await db.MonthBeverages.AsNoTracking()
            .Where(b => b.Month == month)
            .ToDictionaryAsync(b => b.EmployeeId, b => b.Count, ct);

        var rows = employees
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .Select(e => new ReportRow(e.Id, e.Name, days.GetValueOrDefault(e.Id), beverages.GetValueOrDefault(e.Id)))
            .ToList();
        return new ReportResponse(month, rows, new ReportTotal(rows.Sum(r => r.LunchDays), rows.Sum(r => r.Beverages)));
    }

    private static string CsvField(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@')
        {
            value = "'" + value;
        }

        return value.AsSpan().IndexOfAny(";\"\r\n") >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
