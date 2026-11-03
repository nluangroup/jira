namespace Lunch.Api.Data;

/// <summary>A person who registers lunch days and beverages.</summary>
public sealed class Employee
{
    /// <summary>Short lowercase identifier, e.g. <c>anna</c>.</summary>
    public required string Id { get; set; }

    /// <summary>Display name.</summary>
    public required string Name { get; set; }

    /// <summary>Whether the employee may read the monthly reports.</summary>
    public bool IsOfficeManager { get; set; }
}
