using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Tests;

public class BagVerificationServiceTests
{
    [Fact]
    public async Task AC5_ValidTagConfirmsAndIncreasesCountFromZeroToOne()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        Assert.Equal(0, await service.GetLoadedCountAsync(101));
        Assert.True((await service.ConfirmLoadedAsync(101, "DEMO-101-001")).Success);
        Assert.Equal(1, await service.GetLoadedCountAsync(101));
    }

    [Fact]
    public async Task AC4_LowercasePaddedTagIsAccepted()
    {
        await using var db = CreateContext();
        var result = await new BagVerificationService(db, new SpecialHandlingService()).VerifyAsync(101, "  demo-101-001  ");
        Assert.True(result.Success); Assert.Equal("DEMO-101-001", result.Bag!.Tag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AC3_EmptyNullAndWhitespaceTagsAreRejected(string? tag)
    {
        await using var db = CreateContext();
        var result = await new BagVerificationService(db, new SpecialHandlingService()).VerifyAsync(101, tag);
        Assert.False(result.Success); Assert.Equal(VerificationOutcome.EmptyTag, result.Outcome);
        Assert.Equal("Please enter a bag tag", result.Message);
    }

    [Fact]
    public async Task AC8_UnknownTagIsRejected()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        var result = await service.ConfirmLoadedAsync(101, "NOT-A-DATASET-TAG");
        Assert.False(result.Success); Assert.Equal(VerificationOutcome.NotFound, result.Outcome);
        Assert.Equal(0, await service.GetLoadedCountAsync(101));
    }

    [Fact]
    public async Task AC9_BagFromAnotherFlightIsRejectedWithoutChangingOutcome()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        var result = await service.ConfirmLoadedAsync(101, "DEMO-102-001");
        Assert.False(result.Success); Assert.Equal(VerificationOutcome.WrongFlight, result.Outcome);
        Assert.Contains("DEMO102", result.Message); Assert.Equal(BagOutcome.Pending, (await db.Bags.FindAsync(3))!.Outcome);
    }

    [Fact]
    public async Task AC10_DuplicateConfirmIsRejectedAndCountStaysOne()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        Assert.True((await service.ConfirmLoadedAsync(101, "DEMO-101-001")).Success);
        var duplicate = await service.ConfirmLoadedAsync(101, "DEMO-101-001");
        Assert.False(duplicate.Success); Assert.Equal(VerificationOutcome.AlreadyLoaded, duplicate.Outcome);
        Assert.Equal(1, await service.GetLoadedCountAsync(101));
    }

    [Fact]
    public async Task AC11_OpenExceptionIsRejectedAndResolvedExceptionCanLoad()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        var rejected = await service.ConfirmLoadedAsync(101, "DEMO-101-002");
        Assert.Equal(VerificationOutcome.OpenException, rejected.Outcome);
        (await db.Exceptions.FindAsync(1))!.Status = ExceptionStatus.Resolved; await db.SaveChangesAsync();
        Assert.True((await service.ConfirmLoadedAsync(101, "DEMO-101-002")).Success);
    }

    [Fact]
    public async Task AC16_SpecialBagNeedsAcknowledgementThenLoads()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        var rejected = await service.ConfirmLoadedAsync(101, "DEMO-101-003");
        var accepted = await service.ConfirmLoadedAsync(101, "DEMO-101-003", acknowledged: true);
        Assert.Equal(VerificationOutcome.AcknowledgementRequired, rejected.Outcome); Assert.True(accepted.Success);
    }

    [Fact]
    public async Task AC12_RejectedConfirmNeverChangesCount()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        await service.ConfirmLoadedAsync(101, "DEMO-102-001");
        Assert.Equal(0, await service.GetLoadedCountAsync(101));
    }

    [Fact]
    public async Task AC7_FirstConfirmMovesFlightToInProgress()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        await service.ConfirmLoadedAsync(101, "DEMO-101-001");
        db.ChangeTracker.Clear();
        Assert.Equal(LoadingStatus.InProgress, (await db.Flights.FindAsync(101))!.LoadingStatus);
    }

    [Fact]
    public async Task AC15_UnknownFlightIdIsHandledSafely()
    {
        await using var db = CreateContext(); var service = new BagVerificationService(db, new SpecialHandlingService());
        var result = await service.ConfirmLoadedAsync(999, "DEMO-101-001");
        Assert.False(result.Success); Assert.Equal(VerificationOutcome.WrongFlight, result.Outcome);
        Assert.Equal(0, await service.GetLoadedCountAsync(999));
    }

    private static AeroLinkDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AeroLinkDbContext>().UseSqlite("Data Source=:memory:").Options;
        var db = new AeroLinkDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        db.Flights.AddRange(new Flight { FlightId = 101, FlightNumber = "DEMO101", LoadingStatus = LoadingStatus.NotStarted }, new Flight { FlightId = 102, FlightNumber = "DEMO102", LoadingStatus = LoadingStatus.NotStarted });
        db.Bags.AddRange(new Bag { BagId = 1, FlightId = 101, Tag = "DEMO-101-001" }, new Bag { BagId = 2, FlightId = 101, Tag = "DEMO-101-002" }, new Bag { BagId = 3, FlightId = 102, Tag = "DEMO-102-001" }, new Bag { BagId = 4, FlightId = 101, Tag = "DEMO-101-003", HandlingType = "Fragile", HandlingInstruction = "Handle carefully" });
        db.Exceptions.Add(new BagException { ExceptionId = 1, BagId = 2, FlightId = 101, Status = ExceptionStatus.Open }); db.SaveChanges();
        return db;
    }
}
