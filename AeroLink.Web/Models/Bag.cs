namespace AeroLink.Web.Models;

/// <summary>A bag expected on a flight, as listed in the baggage manifest.</summary>
public class Bag
{
    public int BagId { get; set; }
    public int FlightId { get; set; }

    /// <summary>Bag tag, e.g. DEMO-101-001. Unique across all flights.</summary>
    public string Tag { get; set; } = "";

    /// <summary>Handling category: Standard, Fragile, Oversized, Wheelchair or SportingEquipment.</summary>
    public string HandlingType { get; set; } = "Standard";

    /// <summary>Supplied handling instruction shown to the employee.</summary>
    public string HandlingInstruction { get; set; } = "";

    /// <summary>Recorded loading outcome. New bags are Pending.</summary>
    public BagOutcome Outcome { get; set; } = BagOutcome.Pending;

    public Flight? Flight { get; set; }

    /// <summary>True when the bag needs special handling (any type other than Standard).</summary>
    public bool IsSpecial =>
        !string.Equals(HandlingType, "Standard", StringComparison.OrdinalIgnoreCase);
}