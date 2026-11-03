namespace Lunch.Api.Data;

/// <summary>The number of beverages an employee registered for a month.</summary>
public sealed class MonthBeverages
{
    /// <summary>Database key.</summary>
    public int Id { get; set; }

    /// <summary>The employee who registered the count.</summary>
    public required string EmployeeId { get; set; }

    /// <summary>The month as <c>yyyy-MM</c>.</summary>
    public required string Month { get; set; }

    /// <summary>The number of beverages.</summary>
    public int Count { get; set; }
}
