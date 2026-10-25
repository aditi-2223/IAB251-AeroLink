namespace AeroLink.Web.Models;

/// <summary>Represents the employee JSON returned by the external HR API.</summary>
/// <remarks>Values are deserialised from a trusted API response only after a successful HTTP response.</remarks>
public sealed class HrEmployeeDto
{
    /// <summary>Gets or sets the HR employee identifier.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Gets or sets the employee's first name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Gets or sets the employee's last name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Gets or sets the employee's email address.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or sets the employee's HR job title.</summary>
    public string JobTitle { get; init; } = string.Empty;
}
