using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Tests;

/// <summary>
/// Acceptance tests for user story I3: special bags must be acknowledged before loading can be confirmed.
/// </summary>
/// <remarks>
/// These tests run <see cref="BagVerificationService"/> together with the real
/// <see cref="SpecialHandlingService"/> against an in-memory SQLite database. No mock objects are used:
/// both services are fast and deterministic, and mocking <see cref="ISpecialHandlingService"/> would only
/// prove that a call was made, not that a special bag is actually kept from being loaded. Each test is
/// named after the acceptance rule it checks and asserts the stored bag outcome, not just the result.
/// </remarks>
public class SpecialHandlingLoadingTests
{
    private const int FlightId = 201;
    private const string OpenExceptionTag = "I3-FRAGILE-EXCEPTION";

    private static readonly string[] AllHandlingTypes =
    {
        HandlingTypes.Standard,
        HandlingTypes.Fragile,
        HandlingTypes.Oversized,
        HandlingTypes.Wheelchair,
        HandlingTypes.SportingEquipment
    };

    // ---- Special bag detected: verification still lets the handler see the instruction ----

    [Fact]
    public async Task VerifyAsync_SpecialBag_PassesWithoutAcknowledgementAndChangesNothing()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.VerifyAsync(FlightId, TagFor(HandlingTypes.Fragile));

        Assert.True(result.Success);
        Assert.True(result.Bag!.IsSpecial);
        Assert.Equal(BagOutcome.Pending, await GetOutcomeAsync(db, TagFor(HandlingTypes.Fragile)));
    }

    // ---- If the instruction has not been acknowledged, loading confirmation is prevented ----

    [Theory]
    [InlineData(HandlingTypes.Fragile)]
    [InlineData(HandlingTypes.Oversized)]
    [InlineData(HandlingTypes.Wheelchair)]
    [InlineData(HandlingTypes.SportingEquipment)]
    public async Task ConfirmLoadedAsync_SpecialBagNotAcknowledged_IsRejectedAndStaysPending(string handlingType)
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.ConfirmLoadedAsync(FlightId, TagFor(handlingType), acknowledged: false);

        Assert.False(result.Success);
        Assert.Equal(VerificationOutcome.AcknowledgementRequired, result.Outcome);
        Assert.Contains(handlingType, result.Message);
        Assert.Equal(BagOutcome.Pending, await GetOutcomeAsync(db, TagFor(handlingType)));
        Assert.Equal(0, await service.GetLoadedCountAsync(FlightId));
    }

    // ---- Employee acknowledges: loading confirmation is enabled ----

    [Theory]
    [InlineData(HandlingTypes.Fragile)]
    [InlineData(HandlingTypes.Oversized)]
    [InlineData(HandlingTypes.Wheelchair)]
    [InlineData(HandlingTypes.SportingEquipment)]
    public async Task ConfirmLoadedAsync_SpecialBagAcknowledged_IsLoaded(string handlingType)
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.ConfirmLoadedAsync(FlightId, TagFor(handlingType), acknowledged: true);

        Assert.True(result.Success);
        Assert.Equal(VerificationOutcome.Valid, result.Outcome);
        Assert.Equal(BagOutcome.Loaded, await GetOutcomeAsync(db, TagFor(handlingType)));
        Assert.Equal(1, await service.GetLoadedCountAsync(FlightId));
    }

    [Fact]
    public async Task ConfirmLoadedAsync_RejectedThenAcknowledged_LoadsOnTheSecondAttempt()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var tag = TagFor(HandlingTypes.Wheelchair);

        var first = await service.ConfirmLoadedAsync(FlightId, tag, acknowledged: false);
        var second = await service.ConfirmLoadedAsync(FlightId, tag, acknowledged: true);

        Assert.False(first.Success);
        Assert.True(second.Success);
        Assert.Equal(BagOutcome.Loaded, await GetOutcomeAsync(db, tag));
    }

    // ---- Standard bags are unaffected ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfirmLoadedAsync_StandardBag_LoadsWhetherOrNotAcknowledgementIsSent(bool acknowledged)
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.ConfirmLoadedAsync(FlightId, TagFor(HandlingTypes.Standard), acknowledged);

        Assert.True(result.Success);
        Assert.Equal(BagOutcome.Loaded, await GetOutcomeAsync(db, TagFor(HandlingTypes.Standard)));
    }

    // ---- Boundary conditions ----

    [Fact]
    public async Task ConfirmLoadedAsync_AcknowledgingOneBag_DoesNotCoverAnotherSpecialBag()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var acknowledgedBag = await service.ConfirmLoadedAsync(FlightId, TagFor(HandlingTypes.Fragile), acknowledged: true);
        var otherBag = await service.ConfirmLoadedAsync(FlightId, TagFor(HandlingTypes.Oversized), acknowledged: false);

        Assert.True(acknowledgedBag.Success);
        Assert.False(otherBag.Success);
        Assert.Equal(VerificationOutcome.AcknowledgementRequired, otherBag.Outcome);
        Assert.Equal(BagOutcome.Pending, await GetOutcomeAsync(db, TagFor(HandlingTypes.Oversized)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfirmLoadedAsync_SpecialBagWithOpenException_IsRefusedForTheExceptionNotTheAcknowledgement(bool acknowledged)
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.ConfirmLoadedAsync(FlightId, OpenExceptionTag, acknowledged);

        Assert.False(result.Success);
        Assert.Equal(VerificationOutcome.OpenException, result.Outcome);
        Assert.Equal(BagOutcome.Pending, await GetOutcomeAsync(db, OpenExceptionTag));
    }

    private static string TagFor(string handlingType) => $"I3-{handlingType.ToUpperInvariant()}";

    private static IBagVerificationService CreateService(AeroLinkDbContext db) =>
        new BagVerificationService(db, new SpecialHandlingService());

    private static async Task<BagOutcome> GetOutcomeAsync(AeroLinkDbContext db, string tag)
    {
        db.ChangeTracker.Clear();
        return (await db.Bags.AsNoTracking().SingleAsync(b => b.Tag == tag)).Outcome;
    }

    private static AeroLinkDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AeroLinkDbContext>().UseSqlite("Data Source=:memory:").Options;
        var db = new AeroLinkDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        db.Flights.Add(new Flight { FlightId = FlightId, FlightNumber = "I3TEST", LoadingStatus = LoadingStatus.NotStarted });

        var bagId = 1;
        foreach (var handlingType in AllHandlingTypes)
        {
            db.Bags.Add(new Bag
            {
                BagId = bagId++,
                FlightId = FlightId,
                Tag = TagFor(handlingType),
                HandlingType = handlingType,
                HandlingInstruction = $"{handlingType} instruction."
            });
        }

        db.Bags.Add(new Bag
        {
            BagId = bagId,
            FlightId = FlightId,
            Tag = OpenExceptionTag,
            HandlingType = HandlingTypes.Fragile,
            HandlingInstruction = "Fragile instruction."
        });
        db.Exceptions.Add(new BagException
        {
            ExceptionId = 1,
            BagId = bagId,
            FlightId = FlightId,
            Status = ExceptionStatus.Open
        });

        db.SaveChanges();
        return db;
    }
}