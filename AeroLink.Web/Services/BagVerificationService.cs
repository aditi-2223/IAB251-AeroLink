using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services;

/// <summary>Possible outcomes when a handler checks or confirms a baggage tag.</summary>
public enum VerificationOutcome
{
    /// <summary>The bag passed every applicable verification rule.</summary>
    Valid,
    /// <summary>No tag was supplied after whitespace was removed.</summary>
    EmptyTag,
    /// <summary>No bag in the manifest dataset has the supplied tag.</summary>
    NotFound,
    /// <summary>The tag belongs to a bag assigned to another departure flight.</summary>
    WrongFlight,
    /// <summary>The bag has already been confirmed as loaded.</summary>
    AlreadyLoaded,
    /// <summary>The bag has an unresolved reported exception.</summary>
    OpenException,
    /// <summary>A special bag needs its handling instruction acknowledged before loading.</summary>
    AcknowledgementRequired
}

/// <summary>Returns the result of verifying or confirming one bag tag.</summary>
public sealed class VerificationResult
{
    /// <summary>Gets whether the requested operation passed validation and, for confirmation, was saved.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the specific validation outcome for the operation.</summary>
    public VerificationOutcome Outcome { get; init; }

    /// <summary>Gets a handler-facing explanation of the outcome.</summary>
    public string Message { get; init; } = "";

    /// <summary>Gets the matched bag when a tag exists, otherwise <c>null</c>.</summary>
    public Bag? Bag { get; init; }
}

/// <summary>Checks baggage tags against a departure manifest and confirms valid bags as loaded.</summary>
public interface IBagVerificationService
{
    /// <summary>
    /// Checks a tag against a flight manifest without changing persisted data.
    /// </summary>
    /// <param name="flightId">The selected departure flight identifier.</param>
    /// <param name="tag">The handler-entered tag; leading and trailing whitespace are ignored.</param>
    /// <returns>A result describing whether the tag may proceed to confirmation.</returns>
    /// <remarks>Pre-condition: <paramref name="flightId"/> identifies a displayed flight. Post-condition: no database rows change.</remarks>
    Task<VerificationResult> VerifyAsync(int flightId, string? tag);

    /// <summary>
    /// Rechecks a tag and confirms it as loaded when every rule passes.
    /// </summary>
    /// <param name="flightId">The selected departure flight identifier.</param>
    /// <param name="tag">The handler-entered tag; leading and trailing whitespace are ignored.</param>
    /// <param name="acknowledged">Whether special-handling instructions were acknowledged.</param>
    /// <returns>A result describing whether the bag was confirmed.</returns>
    /// <remarks>Pre-condition: none; this method validates directly submitted values. Post-condition: success changes one Pending bag to Loaded at most once.</remarks>
    Task<VerificationResult> ConfirmLoadedAsync(int flightId, string? tag, bool acknowledged = false);

    /// <summary>Counts bags already confirmed as loaded for a flight.</summary>
    /// <param name="flightId">The departure flight identifier.</param>
    /// <returns>The current database count of Loaded bags.</returns>
    /// <remarks>Post-condition: no database rows change.</remarks>
    Task<int> GetLoadedCountAsync(int flightId);

    /// <summary>Counts all expected bags on a flight manifest.</summary>
    /// <param name="flightId">The departure flight identifier.</param>
    /// <returns>The current database count of expected bags.</returns>
    /// <remarks>Post-condition: no database rows change.</remarks>
    Task<int> GetExpectedBagCountAsync(int flightId);
}

/// <inheritdoc cref="IBagVerificationService"/>
public class BagVerificationService : IBagVerificationService
{
    private readonly AeroLinkDbContext _db;
    private readonly ISpecialHandlingService _specialHandling;

    /// <summary>Creates the service with the dependencies supplied by dependency injection.</summary>
    /// <param name="db">The context used to read and update baggage loading data.</param>
    /// <param name="specialHandling">Service that decides whether a special bag may be confirmed.</param>
    public BagVerificationService(AeroLinkDbContext db, ISpecialHandlingService specialHandling)
    {
        _db = db;
        _specialHandling = specialHandling;
    }

    /// <inheritdoc />
    public async Task<VerificationResult> VerifyAsync(int flightId, string? tag) =>
        await CheckAsync(flightId, tag, acknowledged: true, requireAcknowledgement: false);

    /// <inheritdoc />
    public async Task<VerificationResult> ConfirmLoadedAsync(int flightId, string? tag, bool acknowledged = false)
    {
        var check = await CheckAsync(flightId, tag, acknowledged, requireAcknowledgement: true);
        if (!check.Success || check.Bag is null)
            return check;

        // The condition makes a concurrent second confirmation affect zero rows.
        var updated = await _db.Bags
            .Where(b => b.BagId == check.Bag.BagId && b.FlightId == flightId && b.Outcome == BagOutcome.Pending)
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.Outcome, BagOutcome.Loaded));

        if (updated == 0)
            return await CheckAsync(flightId, tag, acknowledged, requireAcknowledgement: true);

        await _db.Flights
            .Where(f => f.FlightId == flightId && f.LoadingStatus == LoadingStatus.NotStarted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.LoadingStatus, LoadingStatus.InProgress));

        check.Bag.Outcome = BagOutcome.Loaded;
        return new VerificationResult
        {
            Success = true,
            Outcome = VerificationOutcome.Valid,
            Message = $"{check.Bag.Tag} confirmed as loaded.",
            Bag = check.Bag
        };
    }

    /// <inheritdoc />
    public Task<int> GetLoadedCountAsync(int flightId) =>
        _db.Bags.AsNoTracking().CountAsync(b => b.FlightId == flightId && b.Outcome == BagOutcome.Loaded);

    /// <inheritdoc />
    public Task<int> GetExpectedBagCountAsync(int flightId) =>
        _db.Bags.AsNoTracking().CountAsync(b => b.FlightId == flightId);

    /// <summary>Checks whether a tag belongs to an eligible bag on the selected flight.</summary>
    /// <param name="flightId">The selected departure flight identifier.</param>
    /// <param name="tag">The tag entered by the handler; surrounding whitespace is ignored.</param>
    /// <param name="acknowledged">Whether the handler acknowledged the special-handling instruction.</param>
    /// <param name="requireAcknowledgement">Whether special bags must have their instruction acknowledged.</param>
    /// <returns>A successful result with the matching bag, or a failure result explaining why the tag cannot proceed.</returns>
    /// <remarks>Post-condition: no database rows change.</remarks>
    private async Task<VerificationResult> CheckAsync(int flightId, string? tag, bool acknowledged, bool requireAcknowledgement)
    {
        var normalisedTag = tag?.Trim();
        if (string.IsNullOrWhiteSpace(normalisedTag))
            return Failure(VerificationOutcome.EmptyTag, "Please enter a bag tag");

        var bag = await _db.Bags.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Tag.ToUpper() == normalisedTag.ToUpper());
        if (bag is null)
            return Failure(VerificationOutcome.NotFound, "Tag not found");

        if (bag.FlightId != flightId)
        {
            var otherFlight = await _db.Flights.AsNoTracking()
                .Where(f => f.FlightId == bag.FlightId)
                .Select(f => f.FlightNumber)
                .FirstOrDefaultAsync();
            return Failure(VerificationOutcome.WrongFlight, $"Tag belongs to flight {otherFlight}", bag);
        }

        if (bag.Outcome == BagOutcome.Loaded)
            return Failure(VerificationOutcome.AlreadyLoaded, "Already loaded", bag);

        var hasOpenException = await _db.Exceptions.AsNoTracking()
            .AnyAsync(e => e.BagId == bag.BagId && e.Status == ExceptionStatus.Open);
        if (hasOpenException)
            return Failure(VerificationOutcome.OpenException, "Unresolved exception, see supervisor", bag);

        if (requireAcknowledgement)
        {
            var decision = _specialHandling.CanConfirmLoading(bag, acknowledged);
            if (!decision.IsAllowed)
                return Failure(VerificationOutcome.AcknowledgementRequired, decision.Message, bag);
        }

        return new VerificationResult { Success = true, Outcome = VerificationOutcome.Valid, Message = "Tag verified", Bag = bag };
    }

    /// <summary>Creates a failed tag-verification result.</summary>
    /// <param name="outcome">The reason verification failed.</param>
    /// <param name="message">The message to show the handler.</param>
    /// <param name="bag">The matched bag, if one was found before verification failed.</param>
    /// <returns>A result whose <see cref="VerificationResult.Success"/> value is <c>false</c>.</returns>
    private static VerificationResult Failure(VerificationOutcome outcome, string message, Bag? bag = null) =>
        new() { Success = false, Outcome = outcome, Message = message, Bag = bag };

}
