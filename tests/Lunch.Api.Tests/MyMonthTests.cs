using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Lunch.Api.Tests;

public sealed class MyMonthTests : IClassFixture<LunchApiFactory>
{
    private sealed record MonthDto(string Month, List<string> LunchDays, int Beverages);

    private readonly WebApplicationFactoryWithClock factory;

    public MyMonthTests(LunchApiFactory baseFactory) => factory = new(baseFactory);

    private HttpClient ClientFor(string employeeId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Employee-Id", employeeId);
        return client;
    }

    private static Task<HttpResponseMessage> PutBeverages(HttpClient client, string month, int count) =>
        client.PutAsJsonAsync($"/api/me/months/{month}/beverages", new { count });

    [Fact]
    public async Task Month_is_empty_for_new_employee()
    {
        var client = ClientFor("lise");

        var month = await client.GetFromJsonAsync<MonthDto>("/api/me/months/2026-01");

        Assert.Equal("2026-01", month!.Month);
        Assert.Empty(month.LunchDays);
        Assert.Equal(0, month.Beverages);
    }

    [Fact]
    public async Task Tick_and_read_back()
    {
        var client = ClientFor("anna");

        var put = await client.PutAsync("/api/me/lunch-days/2026-02-03", null);
        var month = await client.GetFromJsonAsync<MonthDto>("/api/me/months/2026-02");

        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        Assert.Equal(["2026-02-03"], month!.LunchDays);
    }

    [Fact]
    public async Task Tick_twice_is_one_day()
    {
        var client = ClientFor("anna");

        var first = await client.PutAsync("/api/me/lunch-days/2026-03-04", null);
        var second = await client.PutAsync("/api/me/lunch-days/2026-03-04", null);
        var month = await client.GetFromJsonAsync<MonthDto>("/api/me/months/2026-03");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(["2026-03-04"], month!.LunchDays);
    }

    [Fact]
    public async Task Untick_removes_day()
    {
        var client = ClientFor("anna");
        await client.PutAsync("/api/me/lunch-days/2026-04-01", null);
        await client.PutAsync("/api/me/lunch-days/2026-04-02", null);

        var first = await client.DeleteAsync("/api/me/lunch-days/2026-04-01");
        var again = await client.DeleteAsync("/api/me/lunch-days/2026-04-01");
        var month = await client.GetFromJsonAsync<MonthDto>("/api/me/months/2026-04");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal(["2026-04-02"], month!.LunchDays);
    }

    [Theory]
    [InlineData("2026-11-01")]
    [InlineData("2026-10-31")]
    public async Task Weekend_is_400(string date)
    {
        var client = ClientFor("anna");

        var response = await client.PutAsync($"/api/me/lunch-days/{date}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Future_day_is_400()
    {
        var client = ClientFor("anna");

        var tomorrow = await client.PutAsync("/api/me/lunch-days/2026-11-04", null);
        var today = await client.PutAsync("/api/me/lunch-days/2026-11-03", null);

        Assert.Equal(HttpStatusCode.BadRequest, tomorrow.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, today.StatusCode);
    }

    [Fact]
    public async Task Beverages_set_and_read_back()
    {
        var client = ClientFor("bjorn");

        var first = await PutBeverages(client, "2026-05", 3);
        var second = await PutBeverages(client, "2026-05", 5);
        var month = await client.GetFromJsonAsync<MonthDto>("/api/me/months/2026-05");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(5, month!.Beverages);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public async Task Beverages_out_of_range_is_400(int count)
    {
        var client = ClientFor("bjorn");

        var response = await PutBeverages(client, "2026-06", count);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Beverages_future_month_is_400()
    {
        var client = ClientFor("bjorn");

        var future = await PutBeverages(client, "2026-12", 1);
        var current = await PutBeverages(client, "2026-11", 1);

        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, current.StatusCode);
    }

    [Fact]
    public async Task Employees_do_not_see_each_other()
    {
        var anna = ClientFor("anna");
        var soren = ClientFor("soren");
        await anna.PutAsync("/api/me/lunch-days/2026-09-01", null);
        await PutBeverages(anna, "2026-09", 4);

        var month = await soren.GetFromJsonAsync<MonthDto>("/api/me/months/2026-09");

        Assert.Empty(month!.LunchDays);
        Assert.Equal(0, month.Beverages);
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("2026-1")]
    [InlineData("nov")]
    public async Task Malformed_month_is_400(string month)
    {
        var client = ClientFor("anna");

        var response = await client.GetAsync($"/api/me/months/{month}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class WebApplicationFactoryWithClock(LunchApiFactory inner)
    {
        private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 3, 11, 0, 0, TimeSpan.Zero));

        public HttpClient CreateClient() => inner.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(time);
            })).CreateClient();
    }
}
