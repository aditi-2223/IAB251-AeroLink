namespace AeroLink.Web.Models;

/// <summary>Contains the outcome, presentation messages, and optional employee of an HR authentication attempt.</summary>
/// <remarks>Invariant: <see cref="Employee"/> is populated only when <see cref="Outcome"/> is <see cref="HrAuthOutcome.Success"/>.</remarks>
public sealed class HrAuthResult
{
    /// <summary>Gets the categorised result of the authentication attempt.</summary>
    public HrAuthOutcome Outcome { get; init; }

    /// <summary>Gets the user-facing general message for the result.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets the email field validation message, or <c>null</c> when the email is valid.</summary>
    public string? EmailError { get; init; }

    /// <summary>Gets the employee ID field validation message, or <c>null</c> when the ID is valid.</summary>
    public string? EmployeeIdError { get; init; }

    /// <summary>Gets the authenticated employee when authentication succeeds; otherwise <c>null</c>.</summary>
    public HrEmployeeDto? Employee { get; init; }
}
