using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>Records bag problems reported during loading and answers whether a flight still has unresolved ones (I4).</summary>
public interface IExceptionService
{
    /// <summary>Validates a report and, when every rule passes, stores it as a new open exception.</summary>
    /// <param name="flightId">The selected departure flight identifier.</param>
    /// <param name="tag">The bag tag entered by the handler; surrounding whitespace and letter case are ignored.</param>
    /// <param name="category">The problem category; must be one of <see cref="ExceptionCategories.All"/>.</param>
    /// <param name="comment">The handler's description of the problem; required, at most <see cref="ExceptionService.MaxCommentLength"/> characters.</param>
    /// <returns>A result describing whether the problem was recorded; the recorded exception is included on success.</returns>
    /// <remarks>
    /// Pre-condition: none; directly submitted values are validated here.
    /// Post-condition: success adds exactly one <see cref="ExceptionStatus.Open"/> exception stamped with the current UTC time
    /// and leaves the bag's loading outcome unchanged; any failure changes nothing.
    /// Rejected cases: empty tag, invalid category, empty or over-long comment, unknown tag, tag from another flight,
    /// bag already loaded, bag already has an open exception.
    /// </remarks>
    Task<ExceptionReportResult> ReportAsync(int flightId, string? tag, string? category, string? comment);

    /// <summary>Returns the unresolved exceptions for a flight, newest first, with their bags loaded.</summary>
    /// <param name="flightId">The departure flight identifier.</param>
    /// <returns>Open exceptions for that flight only; empty when there are none. Exceptions without a recorded time come last.</returns>
    /// <remarks>Post-condition: no database rows change.</remarks>
    Task<IReadOnlyList<BagException>> GetOpenExceptionsAsync(int flightId);

    /// <summary>Tells whether a flight has any unresolved exception, which must stop loading being marked complete.</summary>
    /// <param name="flightId">The departure flight identifier.</param>
    /// <returns><c>true</c> when at least one open exception exists for the flight; otherwise <c>false</c>.</returns>
    /// <remarks>Post-condition: no database rows change. Exceptions on other flights are ignored.</remarks>
    Task<bool> HasOpenExceptionsAsync(int flightId);
}