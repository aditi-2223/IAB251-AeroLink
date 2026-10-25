namespace AeroLink.Web.Models;

/// <summary>Describes the result of validating an employee against the HR system.</summary>
/// <remarks>Invariant: <see cref="Success"/> is represented only by <see cref="Success"/>.</remarks>
public enum HrAuthOutcome
{
    /// <summary>The employee was validated and holds an allowed baggage role.</summary>
    Success,
    /// <summary>The submitted email or employee ID failed local validation.</summary>
    ValidationFailed,
    /// <summary>The employee ID was unknown or did not match the supplied email.</summary>
    InvalidCredentials,
    /// <summary>The employee exists but does not have an allowed baggage role.</summary>
    NotAuthorised,
    /// <summary>The HR system could not be safely used to verify the employee.</summary>
    ServiceUnavailable
}
