using AeroLink.Web.Models;
using AeroLink.Web.Services;

namespace AeroLink.Tests;

/// <summary>Tests for the small classes that support the HR login (T2): role check, result factories and HR settings.</summary>
public class HrAuthSupportTests
{
    // ---------- HrRoles ----------

    [Theory]
    [InlineData("Baggage Handler", true)]
    [InlineData("BG_Supervisor", true)]
    [InlineData("  baggage handler  ", true)]   // case and surrounding spaces are ignored
    [InlineData("bg_supervisor", true)]
    [InlineData("Developer", false)]
    [InlineData("Baggage Supervisor", false)]   // not the HR system's spelling
    [InlineData("BG Supervisor", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void HrRoles_IsAllowed_AcceptsOnlyTheTwoBaggageRoles(string? jobTitle, bool expected)
    {
        Assert.Equal(expected, HrRoles.IsAllowed(jobTitle));
    }

    // ---------- HrAuthResult ----------

    [Fact]
    public void HrAuthResult_Succeeded_CarriesTheEmployee()
    {
        var employee = new HrEmployeeDto { EmployeeId = 1, FirstName = "A", LastName = "B" };

        var result = HrAuthResult.Succeeded(employee);

        Assert.True(result.IsSuccess);
        Assert.Same(employee, result.Employee);
    }

    [Fact]
    public void HrAuthResult_Succeeded_NullEmployee_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => HrAuthResult.Succeeded(null!));
    }

    [Fact]
    public void HrAuthResult_ValidationFailed_WithNoErrors_Throws()
    {
        Assert.Throws<ArgumentException>(() => HrAuthResult.ValidationFailed(null, null));
    }

    [Theory]
    [InlineData(HrAuthOutcome.Success)]
    [InlineData(HrAuthOutcome.ValidationFailed)]
    public void HrAuthResult_Failed_WithSuccessOrValidationOutcome_Throws(HrAuthOutcome outcome)
    {
        Assert.Throws<ArgumentException>(() => HrAuthResult.Failed(outcome, "message"));
    }

    [Fact]
    public void HrAuthResult_Failed_WithBlankMessage_Throws()
    {
        Assert.Throws<ArgumentException>(() => HrAuthResult.Failed(HrAuthOutcome.NotAuthorised, "  "));
    }

    // ---------- HrEmployeeDto ----------

    [Theory]
    [InlineData("Sarah", "Mitchell", "Sarah Mitchell")]
    [InlineData("Cher", "", "Cher")]
    [InlineData("", "", "")]
    public void HrEmployeeDto_FullName_JoinsNamesWithoutExtraSpaces(string first, string last, string expected)
    {
        var employee = new HrEmployeeDto { FirstName = first, LastName = last };

        Assert.Equal(expected, employee.FullName);
    }

    // ---------- HrApiOptions ----------

    [Theory]
    [InlineData("https://localhost:5001", "https://localhost:5001/")]
    [InlineData("https://localhost:5001/", "https://localhost:5001/")]
    [InlineData("  http://localhost:5000//  ", "http://localhost:5000/")]
    public void HrApiOptions_GetBaseUri_AlwaysEndsWithOneSlash(string baseUrl, string expected)
    {
        var options = new HrApiOptions { BaseUrl = baseUrl };

        Assert.Equal(expected, options.GetBaseUri().ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("localhost:5001/api")]
    public void HrApiOptions_GetBaseUri_InvalidUrl_Throws(string baseUrl)
    {
        var options = new HrApiOptions { BaseUrl = baseUrl };

        Assert.Throws<InvalidOperationException>(() => options.GetBaseUri());
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(0, 5)]
    [InlineData(-3, 5)]
    public void HrApiOptions_GetTimeout_UsesDefaultForNonPositiveValues(int seconds, int expectedSeconds)
    {
        var options = new HrApiOptions { TimeoutSeconds = seconds };

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), options.GetTimeout());
    }
}