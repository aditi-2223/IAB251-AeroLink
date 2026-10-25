namespace AeroLink.Web.Models;

/// <summary>
/// The outcome of asking whether a bag may be confirmed as loaded, with a message suitable for the employee.
/// </summary>
/// <param name="IsAllowed">Whether loading may be confirmed.</param>
/// <param name="Message">A user-friendly explanation of the decision; never blank.</param>
/// <remarks>Post-condition: instances never change after creation.</remarks>
public sealed record LoadingDecision(bool IsAllowed, string Message)
{
    /// <summary>Creates a decision that allows loading to be confirmed.</summary>
    /// <param name="message">The message to show the employee.</param>
    /// <returns>A decision with <see cref="IsAllowed"/> set to <c>true</c>.</returns>
    public static LoadingDecision Allow(string message) => new(true, message);

    /// <summary>Creates a decision that prevents loading from being confirmed.</summary>
    /// <param name="message">The reason to show the employee.</param>
    /// <returns>A decision with <see cref="IsAllowed"/> set to <c>false</c>.</returns>
    public static LoadingDecision Reject(string message) => new(false, message);
}