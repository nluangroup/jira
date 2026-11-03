using System.Net;
using System.Net.Http.Json;

namespace Lunch.Api.Tests;

public sealed class IdentityTests(LunchApiFactory factory) : IClassFixture<LunchApiFactory>
{
    private sealed record EmployeeDto(string Id, string Name, bool IsOfficeManager);

    [Fact]
    public async Task Employees_lists_seeded_employees()
    {
        var client = factory.CreateClient();

        var employees = await client.GetFromJsonAsync<List<EmployeeDto>>("/api/employees");

        Assert.NotNull(employees);
        Assert.True(employees.Count >= 4);
        Assert.Contains(employees, e => e.Name == "Søren Ærø");
        Assert.Single(employees, e => e.IsOfficeManager);
    }

    [Fact]
    public async Task Me_without_header_is_401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_unknown_id_is_401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Employee-Id", "nobody");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_current_employee()
    {
        var client = factory.CreateClient();
        var employees = await client.GetFromJsonAsync<List<EmployeeDto>>("/api/employees");
        var expected = employees![0];
        client.DefaultRequestHeaders.Add("X-Employee-Id", expected.Id);

        var me = await client.GetFromJsonAsync<EmployeeDto>("/api/me");

        Assert.Equal(expected, me);
    }
}
