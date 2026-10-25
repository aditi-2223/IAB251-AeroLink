using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Tests;

/// <summary>Unit tests for <see cref="ExceptionService"/> (user story I4: report a bag problem).</summary>
public class ExceptionServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 12, 9, 30, 0, TimeSpan.Zero);

    // ---- Recording a report (typical case) ----

    [Fact]
    public async Task AC10_ValidReportIsStoredAsOpenWithBagFlightCategoryAndComment()
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, "DEMO-101-001", ExceptionCategories.MissingBag, "Not in the loading area");

        Assert.True(result.Success);
        Assert.Equal(ExceptionReportOutcome.Reported, result.Outcome);
        var saved = await db.Exceptions.SingleAsync(e => e.BagId == 1);
        Assert.Equal(101, saved.FlightId);
        Assert.Equal(ExceptionCategories.MissingBag, saved.Category);
        Assert.Equal("Not in the loading area", saved.Description);
        Assert.Equal(ExceptionStatus.Open, saved.Status);
        Assert.Null(saved.DecisionReason);
    }

    [Fact]
    public async Task AC11_ReportedTimeComesFromTheClockInUtc()
    {
        await using var db = CreateContext();
        await CreateService(db).ReportAsync(101, "DEMO-101-001", ExceptionCategories.DamagedBag, "Torn cover");

        var saved = await db.Exceptions.SingleAsync(e => e.BagId == 1);
        Assert.Equal(FixedNow.UtcDateTime, saved.ReportedAtUtc);
    }

    [Fact]
    public async Task AC13_ReportingDoesNotChangeTheBagOutcome()
    {
        await using var db = CreateContext();
        await CreateService(db).ReportAsync(101, "DEMO-101-001", ExceptionCategories.MissingBag, "Missing");

        Assert.Equal(BagOutcome.Pending, (await db.Bags.FindAsync(1))!.Outcome);
    }

    [Fact]
    public async Task AC4_LowercasePaddedTagAndCategoryAreAcceptedAndNormalised()
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, "  demo-101-001  ", " missingbag ", "  Missing  ");

        Assert.True(result.Success);
        var saved = await db.Exceptions.SingleAsync(e => e.BagId == 1);
        Assert.Equal(ExceptionCategories.MissingBag, saved.Category);
        Assert.Equal("Missing", saved.Description);
    }

    // ---- Tag rules ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AC3_EmptyNullAndWhitespaceTagsAreRejectedAndNothingIsSaved(string? tag)
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, tag, ExceptionCategories.MissingBag, "Missing");

        Assert.False(result.Success);
        Assert.Equal(ExceptionReportOutcome.EmptyTag, result.Outcome);
        Assert.Equal(StartingExceptionCount, await db.Exceptions.CountAsync());
    }

    [Fact]
    public async Task AC4_UnknownTagIsRejected()
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, "NOT-A-TAG", ExceptionCategories.MissingBag, "Missing");

        Assert.Equal(ExceptionReportOutcome.NotFound, result.Outcome);
        Assert.Equal(StartingExceptionCount, await db.Exceptions.CountAsync());
    }

    [Fact]
    public async Task AC4_TagFromAnotherFlightIsRejectedAndNamesThatFlight()
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, "DEMO-102-001", ExceptionCategories.MissingBag, "Missing");

        Assert.Equal(ExceptionReportOutcome.WrongFlight, result.Outcome);
        Assert.Contains("DEMO102", result.Message);
        Assert.Equal(StartingExceptionCount, await db.Exceptions.CountAsync());
    }

    // ---- Comment rules (boundary cases) ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AC5_EmptyNullAndWhitespaceCommentsAreRejected(string? comment)
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, "DEMO-101-001", ExceptionCategories.MissingBag, comment);

        Assert.Equal(ExceptionReportOutcome.EmptyComment, result.Outcome);
        Assert.Equal(StartingExceptionCount, await db.Exceptions.CountAsync());
    }

    [Fact]
    public async Task AC5_CommentOfExactlyTheMaximumLengthIsAccepted()
    {
        await using var db = CreateContext();
        var comment = new string('x', ExceptionService.MaxCommentLength);
        var result = await CreateService(db).ReportAsync(101, "DEMO-101-001", ExceptionCategories.MissingBag, comment);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task AC5_CommentOneCharacterOverTheMaximumIsRejected()
    {
        await using var db = CreateContext();
        var comment = new string('x', ExceptionService.MaxCommentLength + 1);
        var result = await CreateService(db).ReportAsync(101, "DEMO-101-001", ExceptionCategories.MissingBag, comment);

        Assert.Equal(ExceptionReportOutcome.CommentTooLong, result.Outcome);
        Assert.Equal(StartingExceptionCount, await db.Exceptions.CountAsync());
    }

    // ---- Category rules ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Lost")]
    public async Task AC6_MissingOrUnknownCategoryIsRejected(string? category)
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, "DEMO-101-001", category, "Missing");

        Assert.Equal(ExceptionReportOutcome.InvalidCategory, result.Outcome);
        Assert.Equal(StartingExceptionCount, await db.Exceptions.CountAsync());
    }

    // ---- Bag state rules ----

    [Fact]
    public async Task AC14_SecondReportForTheSameBagIsRejectedAndOnlyOneRowExists()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        Assert.True((await service.ReportAsync(101, "DEMO-101-001", ExceptionCategories.MissingBag, "First")).Success);

        var second = await service.ReportAsync(101, "DEMO-101-001", ExceptionCategories.DamagedBag, "Second");

        Assert.Equal(ExceptionReportOutcome.AlreadyReported, second.Outcome);
        Assert.Equal(1, await db.Exceptions.CountAsync(e => e.BagId == 1));
    }

    [Fact]
    public async Task AC14_BagWithASeededOpenExceptionCannotBeReportedAgain()
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, "DEMO-101-002", ExceptionCategories.MissingBag, "Again");

        Assert.Equal(ExceptionReportOutcome.AlreadyReported, result.Outcome);
    }

    [Fact]
    public async Task AC14_BagWhoseEarlierExceptionIsResolvedCanBeReportedAgain()
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(103, "DEMO-103-002", ExceptionCategories.DamagedBag, "Damaged again");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task AC15_BagThatIsAlreadyLoadedIsRejected()
    {
        await using var db = CreateContext();
        var result = await CreateService(db).ReportAsync(101, "DEMO-101-005", ExceptionCategories.MissingBag, "Missing");

        Assert.Equal(ExceptionReportOutcome.AlreadyLoaded, result.Outcome);
        Assert.Equal(StartingExceptionCount, await db.Exceptions.CountAsync());
    }

    // ---- Open exceptions and the completion rule ----

    [Fact]
    public async Task AC20_HasOpenExceptionsIsFalseForAFlightWithNone()
    {
        await using var db = CreateContext();

        Assert.False(await CreateService(db).HasOpenExceptionsAsync(104));
    }

    [Fact]
    public async Task AC20_HasOpenExceptionsBecomesTrueAfterAReport()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        Assert.False(await service.HasOpenExceptionsAsync(104));

        await service.ReportAsync(104, "DEMO-104-001", ExceptionCategories.MissingBag, "Missing");

        Assert.True(await service.HasOpenExceptionsAsync(104));
    }

    [Fact]
    public async Task AC21_AnOpenExceptionOnAnotherFlightDoesNotAffectTheResult()
    {
        await using var db = CreateContext();

        // Flight 102 has an open exception; flight 104 has none.
        Assert.True(await CreateService(db).HasOpenExceptionsAsync(102));
        Assert.False(await CreateService(db).HasOpenExceptionsAsync(104));
    }

    [Fact]
    public async Task AC20_ResolvedExceptionsDoNotCountAsOpen()
    {
        await using var db = CreateContext();

        Assert.False(await CreateService(db).HasOpenExceptionsAsync(103));
    }

    [Fact]
    public async Task AC16_OpenExceptionsAreForTheSelectedFlightOnlyNewestFirstWithTheBagLoaded()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        await service.ReportAsync(101, "DEMO-101-001", ExceptionCategories.MissingBag, "Missing");

        var open = await service.GetOpenExceptionsAsync(101);

        Assert.Equal(2, open.Count);
        Assert.Equal("DEMO-101-001", open[0].Bag!.Tag);   // newly reported, has a time
        Assert.Equal("DEMO-101-002", open[1].Bag!.Tag);   // seeded, no recorded time, so last
        Assert.All(open, e => Assert.Equal(101, e.FlightId));
    }

    [Fact]
    public async Task AC16_OpenExceptionsAreEmptyWhenTheFlightHasNone()
    {
        await using var db = CreateContext();

        Assert.Empty(await CreateService(db).GetOpenExceptionsAsync(104));
    }

    // ---- Integration with bag verification (I2) ----

    [Fact]
    public async Task AC19_ReportedBagCanNoLongerBeConfirmedAsLoaded()
    {
        await using var db = CreateContext();
        await CreateService(db).ReportAsync(101, "DEMO-101-001", ExceptionCategories.MissingBag, "Missing");

        var confirm = await new BagVerificationService(db, new SpecialHandlingService())
            .ConfirmLoadedAsync(101, "DEMO-101-001");

        Assert.False(confirm.Success);
        Assert.Equal(VerificationOutcome.OpenException, confirm.Outcome);
    }

    // ---- Category helpers ----

    [Theory]
    [InlineData("MissingBag", "Missing bag")]
    [InlineData("DamagedBag", "Damaged bag")]
    [InlineData("", "")]
    public void DisplayNameAddsSpacesAndLowercasesTheRest(string category, string expected)
    {
        Assert.Equal(expected, ExceptionCategories.DisplayName(category));
    }

    // ---- Helpers ----

    /// <summary>Exceptions present before a test acts: bag 2 on flight 101, bag 3 on flight 102, one resolved on flight 103.</summary>
    private const int StartingExceptionCount = 3;

    /// <summary>A clock that always returns the same moment, standing in for the system clock.</summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static ExceptionService CreateService(AeroLinkDbContext db) =>
        new(db, new FixedTimeProvider(FixedNow));

    private static AeroLinkDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AeroLinkDbContext>().UseSqlite("Data Source=:memory:").Options;
        var db = new AeroLinkDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        db.Flights.AddRange(
            new Flight { FlightId = 101, FlightNumber = "DEMO101", LoadingStatus = LoadingStatus.NotStarted },
            new Flight { FlightId = 102, FlightNumber = "DEMO102", LoadingStatus = LoadingStatus.NotStarted },
            new Flight { FlightId = 103, FlightNumber = "DEMO103", LoadingStatus = LoadingStatus.NotStarted },
            new Flight { FlightId = 104, FlightNumber = "DEMO104", LoadingStatus = LoadingStatus.NotStarted });

        db.Bags.AddRange(
            new Bag { BagId = 1, FlightId = 101, Tag = "DEMO-101-001" },
            new Bag { BagId = 2, FlightId = 101, Tag = "DEMO-101-002" },
            new Bag { BagId = 3, FlightId = 102, Tag = "DEMO-102-001" },
            new Bag { BagId = 5, FlightId = 101, Tag = "DEMO-101-005", Outcome = BagOutcome.Loaded },
            new Bag { BagId = 6, FlightId = 103, Tag = "DEMO-103-002" },
            new Bag { BagId = 7, FlightId = 104, Tag = "DEMO-104-001" });

        db.Exceptions.AddRange(
            new BagException { ExceptionId = 1, BagId = 2, FlightId = 101, Category = ExceptionCategories.MissingBag, Status = ExceptionStatus.Open },
            new BagException { ExceptionId = 2, BagId = 3, FlightId = 102, Category = ExceptionCategories.MissingBag, Status = ExceptionStatus.Open },
            new BagException { ExceptionId = 3, BagId = 6, FlightId = 103, Category = ExceptionCategories.DamagedBag, Status = ExceptionStatus.Resolved });
        db.SaveChanges();
        return db;
    }
}