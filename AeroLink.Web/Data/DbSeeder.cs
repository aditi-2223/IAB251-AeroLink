using AeroLink.Web.Models;
using ClosedXML.Excel;

namespace AeroLink.Web.Data;

/// <summary>
/// Loads the supplied baggage manifest workbook into the database once, on first run.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Creates the database if it does not exist, and seeds it from <paramref name="xlsxPath"/>
    /// if it is currently empty. Safe to call on every application start.
    /// </summary>
    /// <param name="db">The database context to seed.</param>
    /// <param name="xlsxPath">Full path to the manifest workbook.</param>
    /// <exception cref="FileNotFoundException">
    /// Thrown when <paramref name="xlsxPath"/> does not exist. Callers should ensure the
    /// seed file is deployed alongside the application before calling this method.
    /// </exception>
    /// <remarks>
    /// Pre-condition: the workbook has "Flights", "Bags" and "Exceptions" sheets in the
    /// documented column order. Post-condition: on an empty database, the row counts in
    /// each table match the workbook. An exception row is skipped, with a message written
    /// to the console, if its bag does not actually belong to the flight it names — this
    /// guards against inconsistent source data rather than seeding a bag exception that
    /// could never be resolved by <c>ExceptionService</c>. This method does not modify a
    /// database that already contains flights, so it never overwrites data entered by users.
    /// </remarks>
    public static void Seed(AeroLinkDbContext db, string xlsxPath)
    {
        db.Database.EnsureCreated();
        if (db.Flights.Any())
            return; // Already seeded.

        if (!File.Exists(xlsxPath))
            throw new FileNotFoundException("Baggage manifest not found.", xlsxPath);

        using var workbook = new XLWorkbook(xlsxPath);

        var flights = ReadFlights(workbook);
        var bags = ReadBags(workbook);
        var exceptions = ReadExceptions(workbook, bags);

        db.Flights.AddRange(flights);
        db.Bags.AddRange(bags);
        db.Exceptions.AddRange(exceptions);
        db.SaveChanges();
    }

    /// <summary>Reads every row of the "Flights" sheet into <see cref="Flight"/> entities.</summary>
    /// <param name="workbook">The open manifest workbook.</param>
    private static List<Flight> ReadFlights(XLWorkbook workbook) =>
        workbook.Worksheet("Flights").RowsUsed().Skip(1).Select(row => new Flight
        {
            FlightId = row.Cell(1).GetValue<int>(),
            FlightLabel = row.Cell(2).GetString(),
            FlightNumber = row.Cell(3).GetString(),
            DepartureAirport = row.Cell(4).GetString(),
            ArrivalAirport = row.Cell(5).GetString(),
            ScheduledDepartureUtc = row.Cell(6).GetDateTime(),
            AircraftId = row.Cell(7).GetString(),
            LoadingStatus = Enum.Parse<LoadingStatus>(row.Cell(8).GetString())
        }).ToList();

    /// <summary>Reads every row of the "Bags" sheet into <see cref="Bag"/> entities.</summary>
    /// <param name="workbook">The open manifest workbook.</param>
    private static List<Bag> ReadBags(XLWorkbook workbook) =>
        workbook.Worksheet("Bags").RowsUsed().Skip(1).Select(row => new Bag
        {
            BagId = row.Cell(1).GetValue<int>(),
            FlightId = row.Cell(2).GetValue<int>(),
            Tag = row.Cell(3).GetString().Trim(),
            HandlingType = row.Cell(4).GetString(),
            HandlingInstruction = row.Cell(5).GetString(),
            Outcome = Enum.Parse<BagOutcome>(row.Cell(6).GetString())
        }).ToList();

    /// <summary>
    /// Reads the "Exceptions" sheet, skipping any row whose bag does not belong to the flight it names.
    /// </summary>
    /// <param name="workbook">The open manifest workbook.</param>
    /// <param name="bags">Bags already read from the "Bags" sheet, used to validate each row.</param>
    private static List<BagException> ReadExceptions(XLWorkbook workbook, List<Bag> bags)
    {
        var flightIdByBagId = bags.ToDictionary(b => b.BagId, b => b.FlightId);
        var result = new List<BagException>();

        foreach (var row in workbook.Worksheet("Exceptions").RowsUsed().Skip(1))
        {
            var exception = new BagException
            {
                ExceptionId = row.Cell(1).GetValue<int>(),
                BagId = row.Cell(2).GetValue<int>(),
                FlightId = row.Cell(3).GetValue<int>(),
                Category = row.Cell(4).GetString(),
                Description = row.Cell(5).GetString(),
                Status = Enum.Parse<ExceptionStatus>(row.Cell(6).GetString()),
                DecisionReason = row.Cell(7).IsEmpty() ? null : row.Cell(7).GetString()
            };

            var bagActuallyOnFlight = flightIdByBagId.TryGetValue(exception.BagId, out var flightId)
                                       && flightId == exception.FlightId;
            if (!bagActuallyOnFlight)
            {
                Console.WriteLine(
                    $"[DbSeeder] Skipped exception {exception.ExceptionId}: " +
                    $"bag {exception.BagId} is not on flight {exception.FlightId}.");
                continue;
            }

            result.Add(exception);
        }

        return result;
    }
}