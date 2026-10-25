using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>Lets a handler verify a manifest tag, confirm an eligible bag as loaded, and report a problem with a bag.</summary>
public class LoadingModel : PageModel
{
    private readonly IFlightService _flights;
    private readonly IBagVerificationService _bagVerification;
    private readonly ISpecialHandlingService _specialHandling;
    private readonly IExceptionService _exceptions;

    /// <summary>Creates the page with its flight, bag verification, special-handling and exception services.</summary>
    /// <param name="flights">Service used to read the selected flight.</param>
    /// <param name="bagVerification">Service that validates and confirms bag tags.</param>
    /// <param name="specialHandling">Service that identifies special bags and supplies their instructions.</param>
    /// <param name="exceptions">Service that records bag problems and lists unresolved ones (I4).</param>
    public LoadingModel(
        IFlightService flights,
        IBagVerificationService bagVerification,
        ISpecialHandlingService specialHandling,
        IExceptionService exceptions)
    {
        _flights = flights;
        _bagVerification = bagVerification;
        _specialHandling = specialHandling;
        _exceptions = exceptions;
    }

    /// <summary>Gets the selected departure flight.</summary>
    public Flight? Flight { get; private set; }

    /// <summary>Gets or sets the tag entered by the handler.</summary>
    [BindProperty]
    public string? Tag { get; set; }

    /// <summary>Gets or sets whether the handler ticked the special-handling acknowledgement checkbox.</summary>
    [BindProperty]
    public bool Acknowledged { get; set; }

    /// <summary>Gets the result shown after verification or confirmation.</summary>
    public VerificationResult? Result { get; private set; }

    /// <summary>Gets or sets the bag tag typed into the report-a-problem form (I4).</summary>
    [BindProperty]
    public string? ReportTag { get; set; }

    /// <summary>Gets or sets the problem category chosen in the report-a-problem form (I4).</summary>
    [BindProperty]
    public string? ReportCategory { get; set; }

    /// <summary>Gets or sets the handler's comment in the report-a-problem form (I4).</summary>
    [BindProperty]
    public string? ReportComment { get; set; }

    /// <summary>Gets the result shown after a report was submitted, or <c>null</c> when none was.</summary>
    public ExceptionReportResult? ReportResult { get; private set; }

    /// <summary>Gets the unresolved exceptions for the selected flight, newest first.</summary>
    public IReadOnlyList<BagException> OpenExceptions { get; private set; } = Array.Empty<BagException>();

    /// <summary>Gets the longest comment the report form accepts.</summary>
    public int MaxCommentLength => ExceptionService.MaxCommentLength;

    /// <summary>Gets the current number of bags loaded for the selected flight.</summary>
    public int LoadedCount { get; private set; }

    /// <summary>Gets the number of bags expected on the selected flight.</summary>
    public int ExpectedCount { get; private set; }

    /// <summary>
    /// Gets the bag the handler may confirm next, or <c>null</c> when there is none.
    /// </summary>
    /// <remarks>
    /// A bag is awaiting confirmation when it passed verification, or was refused only because its
    /// special-handling instruction was not acknowledged, and it has not yet been loaded.
    /// </remarks>
    public Bag? BagAwaitingConfirmation =>
        Result is { Bag: { } bag, Outcome: VerificationOutcome.Valid or VerificationOutcome.AcknowledgementRequired }
        && bag.Outcome != BagOutcome.Loaded
            ? bag
            : null;

    /// <summary>Loads the selected flight and its current loading total.</summary>
    /// <param name="flightId">Flight identifier supplied in the query string.</param>
    /// <returns>The page when the flight exists, otherwise a 404 result.</returns>
    public async Task<IActionResult> OnGetAsync(int flightId) => await LoadPageAsync(flightId) ? Page() : NotFound();

    /// <summary>Verifies a tag without changing its loading outcome.</summary>
    /// <param name="flightId">Selected flight identifier.</param>
    /// <returns>The refreshed page or a 404 result for an unknown flight.</returns>
    /// <remarks>Post-condition: the acknowledgement is cleared, so each bag must be acknowledged on its own.</remarks>
    public async Task<IActionResult> OnPostVerifyAsync(int flightId)
    {
        if (!await LoadPageAsync(flightId)) return NotFound();
        Acknowledged = false;
        ModelState.Remove(nameof(Acknowledged));
        Result = await _bagVerification.VerifyAsync(flightId, Tag);
        return Page();
    }

    /// <summary>Confirms a tag as loaded after the service revalidates every rule.</summary>
    /// <param name="flightId">Selected flight identifier.</param>
    /// <returns>The refreshed page or a 404 result for an unknown flight.</returns>
    public async Task<IActionResult> OnPostConfirmAsync(int flightId)
    {
        if (!await LoadPageAsync(flightId)) return NotFound();
        Result = await _bagVerification.ConfirmLoadedAsync(flightId, Tag, Acknowledged);
        LoadedCount = await _bagVerification.GetLoadedCountAsync(flightId);
        return Page();
    }

    /// <summary>Records a problem with a bag as an open exception.</summary>
    /// <param name="flightId">Selected flight identifier.</param>
    /// <returns>The refreshed page or a 404 result for an unknown flight.</returns>
    /// <remarks>
    /// Post-condition: on success the report form is cleared and the new exception is listed; on failure the
    /// handler's entries are kept so they can be corrected. The loading result and count are unaffected.
    /// </remarks>
    public async Task<IActionResult> OnPostReportAsync(int flightId)
    {
        if (!await LoadPageAsync(flightId)) return NotFound();
        ReportResult = await _exceptions.ReportAsync(flightId, ReportTag, ReportCategory, ReportComment);
        if (ReportResult.Success)
        {
            // Tag helpers read ModelState before the property, so the old values must be removed too.
            ModelState.Remove(nameof(ReportTag));
            ModelState.Remove(nameof(ReportCategory));
            ModelState.Remove(nameof(ReportComment));
            ReportTag = null;
            ReportCategory = null;
            ReportComment = null;
        }
        OpenExceptions = await _exceptions.GetOpenExceptionsAsync(flightId);
        return Page();
    }

    /// <summary>Gets the handling category and instruction to display for a bag.</summary>
    /// <param name="bag">The verified bag.</param>
    /// <returns>The bag's handling information, as decided by the special-handling service.</returns>
    public SpecialHandlingInfo GetHandlingInfo(Bag bag) => _specialHandling.GetInfo(bag);

    /// <summary>Loads the selected flight and the values shown alongside it.</summary>
    /// <param name="flightId">The departure flight identifier.</param>
    /// <returns><c>true</c> when the flight exists; otherwise <c>false</c>.</returns>
    /// <remarks>Post-condition: when successful, the flight, counts and open exceptions are refreshed.</remarks>
    private async Task<bool> LoadPageAsync(int flightId)
    {
        Flight = await _flights.GetManifestAsync(flightId);
        if (Flight is null) return false;
        LoadedCount = await _bagVerification.GetLoadedCountAsync(flightId);
        ExpectedCount = await _bagVerification.GetExpectedBagCountAsync(flightId);
        OpenExceptions = await _exceptions.GetOpenExceptionsAsync(flightId);
        return true;
    }
}