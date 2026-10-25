using AeroLink.Web.Models;
using AeroLink.Web.Services;

namespace AeroLink.Tests;

/// <summary>
/// Unit tests for <see cref="SpecialHandlingService"/> (user story I3).
/// </summary>
/// <remarks>
/// The service is pure logic with no dependencies (no database, no HTTP, no clock), so these tests
/// use the real class and plain <see cref="Bag"/> objects. Mock objects are deliberately not used
/// because there is nothing to isolate. Each test is named after the acceptance rule it checks.
/// </remarks>
public class SpecialHandlingServiceTests
{
    private const string SuppliedInstruction = "Keep upright and load last.";

    private readonly ISpecialHandlingService _service = new SpecialHandlingService();

    // ---- GetInfo: identification, category and instruction ----

    [Fact]
    public void GetInfo_StandardBag_IsNotSpecialAndNeedsNoAcknowledgement()
    {
        var bag = CreateBag(HandlingTypes.Standard, "Standard handling.");

        var info = _service.GetInfo(bag);

        Assert.False(info.IsSpecial);
        Assert.False(info.RequiresAcknowledgement);
        Assert.Equal(HandlingTypes.Standard, info.Category);
        Assert.Equal("Standard handling.", info.Instruction);
    }

    [Theory]
    [InlineData(HandlingTypes.Fragile)]
    [InlineData(HandlingTypes.Oversized)]
    [InlineData(HandlingTypes.Wheelchair)]
    [InlineData(HandlingTypes.SportingEquipment)]
    public void GetInfo_SpecialBag_ReturnsCategoryAndSuppliedInstruction(string handlingType)
    {
        var bag = CreateBag(handlingType, SuppliedInstruction);

        var info = _service.GetInfo(bag);

        Assert.True(info.IsSpecial);
        Assert.True(info.RequiresAcknowledgement);
        Assert.Equal(handlingType, info.Category);
        Assert.Equal(SuppliedInstruction, info.Instruction);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetInfo_SpecialBagWithBlankInstruction_UsesDefaultInstruction(string blankInstruction)
    {
        var bag = CreateBag(HandlingTypes.Fragile, blankInstruction);

        var info = _service.GetInfo(bag);

        Assert.False(string.IsNullOrWhiteSpace(info.Instruction));
        Assert.NotEqual(blankInstruction, info.Instruction);
    }

    [Fact]
    public void GetInfo_StandardBagWithBlankInstruction_UsesStandardFallback()
    {
        var bag = CreateBag(HandlingTypes.Standard, "");

        var info = _service.GetInfo(bag);

        Assert.Equal("Standard handling.", info.Instruction);
    }

    [Fact]
    public void GetInfo_InstructionWithSurroundingWhitespace_IsTrimmed()
    {
        var bag = CreateBag(HandlingTypes.Oversized, "  " + SuppliedInstruction + "  ");

        var info = _service.GetInfo(bag);

        Assert.Equal(SuppliedInstruction, info.Instruction);
    }

    [Theory]
    [InlineData("fragile", true)]
    [InlineData("FRAGILE", true)]
    [InlineData("standard", false)]
    [InlineData("STANDARD", false)]
    public void GetInfo_HandlingTypeComparisonIsCaseInsensitive(string handlingType, bool expectedSpecial)
    {
        var bag = CreateBag(handlingType, SuppliedInstruction);

        var info = _service.GetInfo(bag);

        Assert.Equal(expectedSpecial, info.IsSpecial);
    }

    [Fact]
    public void GetInfo_BlankHandlingType_IsTreatedAsSpecialSoNoCheckIsSkipped()
    {
        var bag = CreateBag("", SuppliedInstruction);

        var info = _service.GetInfo(bag);

        Assert.True(info.IsSpecial);
        Assert.False(string.IsNullOrWhiteSpace(info.Category));
    }

    // ---- CanConfirmLoading: acknowledgement rule ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanConfirmLoading_StandardBag_IsAllowedWhetherOrNotAcknowledged(bool acknowledged)
    {
        var bag = CreateBag(HandlingTypes.Standard, "Standard handling.");

        var decision = _service.CanConfirmLoading(bag, acknowledged);

        Assert.True(decision.IsAllowed);
    }

    [Theory]
    [InlineData(HandlingTypes.Fragile)]
    [InlineData(HandlingTypes.Oversized)]
    [InlineData(HandlingTypes.Wheelchair)]
    [InlineData(HandlingTypes.SportingEquipment)]
    public void CanConfirmLoading_SpecialBagNotAcknowledged_IsRejected(string handlingType)
    {
        var bag = CreateBag(handlingType, SuppliedInstruction);

        var decision = _service.CanConfirmLoading(bag, acknowledged: false);

        Assert.False(decision.IsAllowed);
        Assert.Contains(handlingType, decision.Message);
    }

    [Theory]
    [InlineData(HandlingTypes.Fragile)]
    [InlineData(HandlingTypes.Oversized)]
    [InlineData(HandlingTypes.Wheelchair)]
    [InlineData(HandlingTypes.SportingEquipment)]
    public void CanConfirmLoading_SpecialBagAcknowledged_IsAllowed(string handlingType)
    {
        var bag = CreateBag(handlingType, SuppliedInstruction);

        var decision = _service.CanConfirmLoading(bag, acknowledged: true);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void CanConfirmLoading_SpecialBagWithBlankInstructionNotAcknowledged_IsStillRejected()
    {
        var bag = CreateBag(HandlingTypes.Wheelchair, "");

        var decision = _service.CanConfirmLoading(bag, acknowledged: false);

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void CanConfirmLoading_Decision_AlwaysCarriesAMessage()
    {
        var bag = CreateBag(HandlingTypes.Fragile, SuppliedInstruction);

        var rejected = _service.CanConfirmLoading(bag, acknowledged: false);
        var allowed = _service.CanConfirmLoading(bag, acknowledged: true);

        Assert.False(string.IsNullOrWhiteSpace(rejected.Message));
        Assert.False(string.IsNullOrWhiteSpace(allowed.Message));
    }

    // ---- Guard clauses ----

    [Fact]
    public void GetInfo_NullBag_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.GetInfo(null!));
    }

    [Fact]
    public void CanConfirmLoading_NullBag_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.CanConfirmLoading(null!, acknowledged: true));
    }

    private static Bag CreateBag(string handlingType, string instruction) =>
        new() { Tag = "TEST-001", HandlingType = handlingType, HandlingInstruction = instruction };
}