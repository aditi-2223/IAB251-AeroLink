using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <inheritdoc cref="IExceptionService"/>
public class ExceptionService : IExceptionService
{
    /// <summary>The longest comment, in characters, that a report may contain.</summary>
    public const int MaxCommentLength = 500;

    private readonly AeroLinkDbContext _db;
    private readonly TimeProvider _time;

    /// <summary>Creates the service with the dependencies supplied by dependency injection.</summary>
    /// <param name="db">The context used to read bags and store exceptions.</param>
    /// <param name="timeProvider">Clock used to stamp reports; the system clock when <c>null</c>. Tests supply a fixed clock.</param>
    public ExceptionService(AeroLinkDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<ExceptionReportResult> ReportAsync(int flightId, string? tag, string? category, string? comment)
    {
        // Checks that need no database access come first.
        var normalisedTag = tag?.Trim();
        if (string.IsNullOrWhiteSpace(normalisedTag))
            return Failure(ExceptionReportOutcome.EmptyTag, "Please enter a bag tag");

        var normalisedCategory = ExceptionCategories.Normalise(category);
        if (normalisedCategory is null)
            return Failure(ExceptionReportOutcome.InvalidCategory, "Please choose the type of problem");

        var normalisedComment = comment?.Trim();
        if (string.IsNullOrWhiteSpace(normalisedComment))
            return Failure(ExceptionReportOutcome.EmptyComment, "Please describe the problem");
        if (normalisedComment.Length > MaxCommentLength)
            return Failure(ExceptionReportOutcome.CommentTooLong,
                $"Comment must be {MaxCommentLength} characters or fewer");

        var bag = await _db.Bags.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Tag.ToUpper() == normalisedTag.ToUpper());
        if (bag is null)
            return Failure(ExceptionReportOutcome.NotFound, "Tag not found");

        if (bag.FlightId != flightId)
        {
            var otherFlight = await _db.Flights.AsNoTracking()
                .Where(f => f.FlightId == bag.FlightId)
                .Select(f => f.FlightNumber)
                .FirstOrDefaultAsync();
            return Failure(ExceptionReportOutcome.WrongFlight, $"Tag belongs to flight {otherFlight}");
        }

        if (bag.Outcome == BagOutcome.Loaded)
            return Failure(ExceptionReportOutcome.AlreadyLoaded, "Bag is already loaded, so it cannot be reported");

        var alreadyReported = await _db.Exceptions.AsNoTracking()
            .AnyAsync(e => e.BagId == bag.BagId && e.Status == ExceptionStatus.Open);
        if (alreadyReported)
            return Failure(ExceptionReportOutcome.AlreadyReported, "This bag already has an open exception");

        var exception = new BagException
        {
            BagId = bag.BagId,
            FlightId = flightId,
            Category = normalisedCategory,
            Description = normalisedComment,
            Status = ExceptionStatus.Open,
            ReportedAtUtc = _time.GetUtcNow().UtcDateTime
        };
        _db.Exceptions.Add(exception);
        await _db.SaveChangesAsync();

        return new ExceptionReportResult(ExceptionReportOutcome.Reported,
            $"Problem reported for {bag.Tag}. A supervisor will investigate.", exception);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BagException>> GetOpenExceptionsAsync(int flightId) =>
        await _db.Exceptions.AsNoTracking()
            .Include(e => e.Bag)
            .Where(e => e.FlightId == flightId && e.Status == ExceptionStatus.Open)
            .OrderByDescending(e => e.ReportedAtUtc)
            .ThenByDescending(e => e.ExceptionId)
            .ToListAsync();

    /// <inheritdoc />
    public Task<bool> HasOpenExceptionsAsync(int flightId) =>
        _db.Exceptions.AsNoTracking().AnyAsync(e => e.FlightId == flightId && e.Status == ExceptionStatus.Open);

    private static ExceptionReportResult Failure(ExceptionReportOutcome outcome, string message) =>
        new(outcome, message);
}