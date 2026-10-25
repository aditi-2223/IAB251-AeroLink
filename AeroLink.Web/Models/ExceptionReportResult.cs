namespace AeroLink.Web.Models;

/// <summary>
/// The outcome of one attempt to report a bag problem, with a message suitable for the handler.
/// </summary>
/// <param name="Outcome">The specific outcome of the attempt.</param>
/// <param name="Message">A user-friendly explanation of the outcome; never blank.</param>
/// <param name="Exception">The exception that was recorded, or <c>null</c> when nothing was saved.</param>
/// <remarks>Post-condition: instances never change after creation.</remarks>
public sealed record ExceptionReportResult(ExceptionReportOutcome Outcome, string Message, BagException? Exception = null)
{
    /// <summary>Gets whether the problem was recorded.</summary>
    public bool Success => Outcome == ExceptionReportOutcome.Reported;
}