namespace AeroLink.Web.Models;

/// <summary>A departure flight whose baggage is loaded by AeroLink staff.</summary>
public class Flight
{
    /// <summary>Unique flight identifier from the manifest.</summary>
    public int FlightId { get; set; }

    /// <summary>Short label (A to J) used in the dataset.</summary>
    public string FlightLabel { get; set; } = "";

    /// <summary>Flight number, e.g. DEMO101.</summary>
    public string FlightNumber { get; set; } = "";

    /// <summary>Airport the flight departs from.</summary>
    public string DepartureAirport { get; set; } = "";
    /// <summary>Airport the flight arrives at.</summary>
    public string ArrivalAirport { get; set; } = "";

    /// <summary>Scheduled departure time in UTC.</summary>
    public DateTime ScheduledDepartureUtc { get; set; }

    /// <summary>Identifier of the aircraft assigned to the flight.</summary>
    public string AircraftId { get; set; } = "";

    /// <summary>Current state of the loading operation.</summary>
    public LoadingStatus LoadingStatus { get; set; }

    /// <summary>Bags expected on this flight.</summary>
    public List<Bag> Bags { get; set; } = new();
}