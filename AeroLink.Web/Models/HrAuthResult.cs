namespace AeroLink.Web.Models;

/// <summary>The result of one attempt to sign in through the HR system (T2), with messages ready to show the employee.</summary>
/// <remarks>
/// Invariants: <see cref="Employee"/> is not <c>null</c> exactly when <see cref="Outcome"/> is
/// <see cref="HrAuthOutcome.Success"/>; <see cref="Message"/> is never blank for a failure;
/// <see cref="EmailError"/> and <see cref="EmployeeIdError"/> are only set when the outcome is
/// <see cref="HrAuthOutcome.ValidationFailed"/>. Instances never change after creation.
/// </remarks>
public sealed class HrAuthResult
{
    /// <summary>The one message used when the Employee ID is unknown or the email does not match. It never says which was wrong.</summary>
    public const string InvalidCredentialsMessage = "Invalid email or employee ID.";

    /// <summary>The message shown when the employee's role may not use the Baggage module.</summary>
    public const string NotAuthorisedMessage = "You do not have permission to access the Baggage module.";

    /// <summary>The message shown when the HR system cannot be used to check the employee.</summary>
    public const string ServiceUnavailableMessage = "Unable to verify your details right now. Please try again later.";

    /// <summary>The message shown when the form is submitted with some fields in error.</summary>
    public const string ValidationFailedMessage = "Please correct the highlighted fields and try again.";

    /// <summary>Gets what happened.</summary>
    public HrAuthOutcome Outcome { get; private init; }

    /// <summary>Gets whether the employee was verified and may be signed in.</summary>
    public bool IsSuccess => Outcome == HrAuthOutcome.Success;

    /// <summary>Gets a message suitable for showing to the employee.</summary>
    public string Message { get; private init; } = string.Empty;

    /// <summary>Gets the problem with the email field, or <c>null</c> if the email was acceptable.</summary>
    public string? EmailError { get; private init; }

    /// <summary>Gets the problem with the Employee ID field, or <c>null</c> if the ID was acceptable.</summary>
    public string? EmployeeIdError { get; private init; }

    /// <summary>Gets the verified employee on success, otherwise <c>null</c>.</summary>
    public HrEmployeeDto? Employee { get; private init; }

    /// <summary>Creates a result for an employee who was verified and has an allowed role.</summary>
    /// <param name="employee">The verified employee.</param>
    /// <returns>A successful result carrying <paramref name="employee"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="employee"/> is <c>null</c>.</exception>
    public static HrAuthResult Succeeded(HrEmployeeDto employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        return new HrAuthResult { Outcome = HrAuthOutcome.Success, Message = "Signed in.", Employee = employee };
    }

    /// <summary>Creates a result for a form that was empty or in the wrong format.</summary>
    /// <param name="emailError">The problem with the email, or <c>null</c> if it was fine.</param>
    /// <param name="employeeIdError">The problem with the Employee ID, or <c>null</c> if it was fine.</param>
    /// <returns>A <see cref="HrAuthOutcome.ValidationFailed"/> result.</returns>
    /// <exception cref="ArgumentException">Both errors are <c>null</c>, because then nothing failed validation.</exception>
    public static HrAuthResult ValidationFailed(string? emailError, string? employeeIdError)
    {
        if (emailError is null && employeeIdError is null)
            throw new ArgumentException("At least one field error is required.");

        return new HrAuthResult
        {
            Outcome = HrAuthOutcome.ValidationFailed,
            Message = ValidationFailedMessage,
            EmailError = emailError,
            EmployeeIdError = employeeIdError
        };
    }

    /// <summary>Creates a result for any failure that is not a validation problem.</summary>
    /// <param name="outcome">The kind of failure; must not be <see cref="HrAuthOutcome.Success"/> or <see cref="HrAuthOutcome.ValidationFailed"/>.</param>
    /// <param name="message">The message to show the employee.</param>
    /// <returns>A failed result with no employee.</returns>
    /// <exception cref="ArgumentException"><paramref name="outcome"/> is Success or ValidationFailed, or <paramref name="message"/> is blank.</exception>
    public static HrAuthResult Failed(HrAuthOutcome outcome, string message)
    {
        if (outcome is HrAuthOutcome.Success or HrAuthOutcome.ValidationFailed)
            throw new ArgumentException("Use Succeeded or ValidationFailed for this outcome.", nameof(outcome));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("A failure needs a message.", nameof(message));

        return new HrAuthResult { Outcome = outcome, Message = message };
    }
}