using System.Net;
using System.Net.Http.Json;
using System.Text;
using Lunch.Api.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Lunch.Api.Tests;

public sealed class ReportTests(LunchApiFactory factory) : IClassFixture<LunchApiFactory>
{
    private sealed record RowDto(string EmployeeId, string Name, int LunchDays, int Beverages);

    private sealed record TotalDto(int LunchDays, int Beverages);

    private sealed record ReportDto(string Month, List<RowDto> Rows, TotalDto Total);

    private HttpClient ClientFor(string employeeId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Employee-Id", employeeId);
        return client;
    }

    private async Task SeedAsync(Action<LunchDbContext> seed)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LunchDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Report_counts_per_employee_and_total()
    {
        await SeedAsync(db =>
        {
            db.LunchDays.AddRange(
                new LunchDay { EmployeeId = "anna", Date = new DateOnly(2026, 3, 2) },
                new LunchDay { EmployeeId = "anna", Date = new DateOnly(2026, 3, 31) },
                new LunchDay { EmployeeId = "anna", Date = new DateOnly(2026, 4, 1) },
                new LunchDay { EmployeeId = "soren", Date = new DateOnly(2026, 3, 3) });
            db.MonthBeverages.AddRange(
                new MonthBeverages { EmployeeId = "anna", Month = "2026-03", Count = 5 },
                new MonthBeverages { EmployeeId = "soren", Month = "2026-03", Count = 2 },
                new MonthBeverages { EmployeeId = "soren", Month = "2026-04", Count = 9 });
        });

        var report = await ClientFor("anna").GetFromJsonAsync<ReportDto>("/api/reports/2026-03");

        Assert.Equal("2026-03", report!.Month);
        Assert.Equal(new RowDto("anna", "Anna Jensen", 2, 5), report.Rows.Single(r => r.EmployeeId == "anna"));
        Assert.Equal(new RowDto("soren", "Søren Ærø", 1, 2), report.Rows.Single(r => r.EmployeeId == "soren"));
        Assert.Equal(new TotalDto(3, 7), report.Total);
        Assert.Equal(["Anna Jensen", "Bjørn Åkesson", "Lise Møller", "Søren Ærø"], report.Rows.Where(r => r.EmployeeId is "anna" or "soren" or "bjorn" or "lise").Select(r => r.Name));
    }

    [Fact]
    public async Task Employee_without_registrations_is_zero_row()
    {
        var report = await ClientFor("anna").GetFromJsonAsync<ReportDto>("/api/reports/2026-05");

        Assert.Equal(new RowDto("lise", "Lise Møller", 0, 0), report!.Rows.Single(r => r.EmployeeId == "lise"));
        Assert.Equal(new TotalDto(0, 0), report.Total);
    }

    [Fact]
    public async Task Non_manager_is_403()
    {
        var client = ClientFor("soren");

        var report = await client.GetAsync("/api/reports/2026-03");
        var csv = await client.GetAsync("/api/reports/2026-03/csv");

        Assert.Equal(HttpStatusCode.Forbidden, report.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, csv.StatusCode);
    }

    [Fact]
    public async Task Csv_has_bom_semicolons_and_header()
    {
        await SeedAsync(db =>
        {
            db.LunchDays.Add(new LunchDay { EmployeeId = "bjorn", Date = new DateOnly(2026, 6, 1) });
            db.MonthBeverages.Add(new MonthBeverages { EmployeeId = "bjorn", Month = "2026-06", Count = 4 });
        });

        var response = await ClientFor("anna").GetAsync("/api/reports/2026-06/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("lunch-2026-06.csv", response.Content.Headers.ContentDisposition!.FileName);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition.DispositionType);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Employee;Lunch days;Beverages", lines[0]);
        Assert.Contains("Bjørn Åkesson;1;4", lines);
        Assert.Contains("Lise Møller;0;0", lines);
    }

    [Fact]
    public async Task Csv_escapes_formula_names()
    {
        await SeedAsync(db =>
        {
            db.Employees.AddRange(
                new Employee { Id = "eq", Name = "=SUM(A1)" },
                new Employee { Id = "semi", Name = "Doe; John" });
        });

        var response = await ClientFor("anna").GetAsync("/api/reports/2026-07/csv");
        var text = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync()).TrimStart('﻿');
        var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("'=SUM(A1);0;0", lines);
        Assert.Contains("\"Doe; John\";0;0", lines);
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("2026-3")]
    [InlineData("nope")]
    public async Task Malformed_month_is_400(string month)
    {
        var client = ClientFor("anna");

        var report = await client.GetAsync($"/api/reports/{month}");
        var csv = await client.GetAsync($"/api/reports/{month}/csv");

        Assert.Equal(HttpStatusCode.BadRequest, report.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, csv.StatusCode);
    }
}
