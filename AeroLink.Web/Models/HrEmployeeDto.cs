namespace AeroLink.Web.Models;

/// <summary>
/// An employee record as returned by the HR system's <c>GET /api/employees/{id}</c> endpoint.
/// Property names match the API's camelCase JSON, which <c>System.Net.Http.Json</c> reads case-insensitively.
/// </summary>
/// <remarks>
/// Invariant: this is a read-only copy of HR data. AeroLink never stores or changes it; it is only used to
/// decide whether to sign the employee in. Only the fields AeroLink needs are listed; the API's other fields
/// (phone, hire date, department) are ignored when the JSON is read.
/// </remarks>
public sealed class HrEmployeeDto
{
    /// <summary>Gets or sets the employee's unique number in the HR system.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Gets or sets the employee's given name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the employee's family name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the employee's work email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the employee's job title, which AeroLink treats as the employee's role.</summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Gets the employee's full name for display, for example "Sarah Mitchell".</summary>
    /// <remarks>Post-condition: never has leading or trailing spaces.</remarks>
    public string FullName => $"{FirstName} {LastName}".Trim();
}