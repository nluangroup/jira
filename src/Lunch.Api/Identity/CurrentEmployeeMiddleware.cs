using Lunch.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Lunch.Api.Identity;

/// <summary>Resolves the employee named by <c>X-Employee-Id</c> for every <c>/api</c> request except the employee list.</summary>
public sealed class CurrentEmployeeMiddleware(RequestDelegate next)
{
    /// <summary>Header carrying the demo identity.</summary>
    public const string HeaderName = "X-Employee-Id";

    /// <summary>Stores the employee in <see cref="HttpContext.Items"/>, or answers 401.</summary>
    public async Task InvokeAsync(HttpContext context, LunchDbContext db)
    {
        var path = context.Request.Path;
        var needsEmployee = path.StartsWithSegments("/api") && !path.Equals("/api/employees");
        if (!needsEmployee)
        {
            await next(context);
            return;
        }

        var id = context.Request.Headers[HeaderName].ToString();
        var employee = id.Length == 0
            ? null
            : await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, context.RequestAborted);
        if (employee is null)
        {
            await Results.Problem(
                title: "Unknown employee",
                detail: $"Send a known employee id in the {HeaderName} header.",
                statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context);
            return;
        }

        context.Items[typeof(Employee)] = employee;
        await next(context);
    }
}

/// <summary>Access to the employee resolved by <see cref="CurrentEmployeeMiddleware"/>.</summary>
public static class HttpContextEmployeeExtensions
{
    /// <summary>Returns the current employee; only valid on endpoints behind the middleware.</summary>
    public static Employee GetCurrentEmployee(this HttpContext context) => (Employee)context.Items[typeof(Employee)]!;
}
