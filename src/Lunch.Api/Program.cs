using Lunch.Api.Data;
using Lunch.Api.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<LunchDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Lunch") ?? "Data Source=lunch.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await EmployeeSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<LunchDbContext>(), app.Configuration, CancellationToken.None);
}

app.UseMiddleware<CurrentEmployeeMiddleware>();
app.MapIdentityEndpoints();

app.Run();

/// <summary>Entry point, public so tests can host the API.</summary>
public partial class Program;
