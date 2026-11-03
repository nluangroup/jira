namespace Lunch.Api.Data;

/// <summary>A date on which an employee ate lunch.</summary>
public sealed class LunchDay
{
    /// <summary>Database key.</summary>
    public int Id { get; set; }

    /// <summary>The employee who registered the day.</summary>
    public required string EmployeeId { get; set; }

    /// <summary>The lunch date.</summary>
    public DateOnly Date { get; set; }
}
