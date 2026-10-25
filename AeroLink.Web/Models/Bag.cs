namespace AeroLink.Web.Models;

/// <summary>A bag expected on a flight, as listed in the baggage manifest.</summary>
public class Bag
{
    /// <summary>Gets or sets the unique bag identifier (primary key).</summary>
    public int BagId { get; set; }

    /// <summary>Gets or sets the identifier of the flight the bag is expected on.</summary>
    public int FlightId { get; set; }

    /// <summary>Bag tag, e.g. DEMO-101-001. Unique across all flights.</summary>
    public string Tag { get; set; } = "";

    /// <summary>
    /// Handling category: Standard, Fragile, Oversized, Wheelchair or SportingEquipment.
    /// See <see cref="HandlingTypes"/>.
    /// </summary>
    public string HandlingType { get; set; } = HandlingTypes.Standard;

    /// <summary>Supplied handling instruction shown to the employee.</summary>
    public string HandlingInstruction { get; set; } = "";

    /// <summary>Recorded loading outcome. New bags are Pending.</summary>
    public BagOutcome Outcome { get; set; } = BagOutcome.Pending;

    /// <summary>Gets or sets the flight this bag belongs to (navigation property).</summary>
    public Flight? Flight { get; set; }

    /// <summary>True when the bag needs special handling (any type other than Standard).</summary>
    /// <remarks>This is the single place that defines what counts as special baggage.</remarks>
    public bool IsSpecial =>
        !string.Equals(HandlingType, HandlingTypes.Standard, StringComparison.OrdinalIgnoreCase);
}