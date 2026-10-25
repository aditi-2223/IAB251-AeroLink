using System.Net;
using System.Text;
using System.Text.Json;
using AeroLink.Web.Models;
using AeroLink.Web.Services;

namespace AeroLink.Tests;

/// <summary>
/// Tests for the HR login (T2). The real HR API is never called: a fake <see cref="HttpMessageHandler"/> answers like the
/// HR system does, using the same employees as its database (ID, email and job title).
/// </summary>
public class HrAuthServiceTests
{
    // ---------- Typical cases: successful sign-in (AC1, AC2) ----------

    [Theory]
    [InlineData("s.mitchell@company.com", "100101", "Sarah Mitchell", HrRoles.BaggageHandler)]
    [InlineData("p.sharma@company.com", "300100", "Priya Sharma", HrRoles.BaggageHandler)]
    [InlineData("j.cooper@company.com", "200100", "James Cooper", HrRoles.BaggageSupervisor)]
    public async Task AC1_AC2_ValidHandlerOrSupervisor_ReturnsSuccessWithEmployee(string email, string id, string fullName, string role)
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync(email, id);

        Assert.True(result.IsSuccess);
        Assert.Equal(HrAuthOutcome.Success, result.Outcome);
        Assert.NotNull(result.Employee);
        Assert.Equal(int.Parse(id), result.Employee!.EmployeeId);
        Assert.Equal(fullName, result.Employee.FullName);
        Assert.Equal(role, result.Employee.JobTitle);
        Assert.Null(result.EmailError);
        Assert.Null(result.EmployeeIdError);
    }

    [Fact]
    public async Task AC1_ValidLogin_MakesExactlyOneGetRequestToTheEmployeeEndpoint()
    {
        var hr = new FakeHr();

        await hr.Service.AuthenticateAsync("s.mitchell@company.com", "100101");

        var request = Assert.Single(hr.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/employees/100101", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AC1_EmailCaseAndSurroundingSpaces_AreIgnored()
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync("  S.Mitchell@Company.COM  ", "  100101  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("/api/employees/100101", Assert.Single(hr.Requests).RequestUri!.AbsolutePath);
    }

    // ---------- AC3, AC4: employee not found, or email does not match ----------

    [Fact]
    public async Task AC3_UnknownEmployeeId_ReturnsInvalidCredentials()
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync("nobody@company.com", "999999");

        Assert.False(result.IsSuccess);
        Assert.Equal(HrAuthOutcome.InvalidCredentials, result.Outcome);
        Assert.Equal(HrAuthResult.InvalidCredentialsMessage, result.Message);
        Assert.Null(result.Employee);
    }

    [Theory]
    [InlineData("wrong@company.com", "100101")]
    [InlineData("j.cooper@company.com", "100101")]   // a real email, but it belongs to a different employee
    public async Task AC4_EmailDoesNotMatchTheEmployee_ReturnsInvalidCredentials(string email, string id)
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync(email, id);

        Assert.Equal(HrAuthOutcome.InvalidCredentials, result.Outcome);
        Assert.Null(result.Employee);
    }

    [Fact]
    public async Task AC4_UnknownIdAndWrongEmail_GiveTheSameMessage_SoNoAccountIsRevealed()
    {
        var hr = new FakeHr();

        var unknownId = await hr.Service.AuthenticateAsync("s.mitchell@company.com", "999999");
        var wrongEmail = await hr.Service.AuthenticateAsync("wrong@company.com", "100101");

        Assert.Equal(unknownId.Outcome, wrongEmail.Outcome);
        Assert.Equal(unknownId.Message, wrongEmail.Message);
    }

    // ---------- AC5: role not allowed ----------

    [Theory]
    [InlineData("e.johnson@company.com", "500100")]  // Developer
    [InlineData("o.brown@company.com", "700100")]    // HR Manager
    [InlineData("e.anderson@company.com", "900100")] // Finance Manager
    public async Task AC5_ExistingEmployeeWithOtherRole_ReturnsNotAuthorised(string email, string id)
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync(email, id);

        Assert.False(result.IsSuccess);
        Assert.Equal(HrAuthOutcome.NotAuthorised, result.Outcome);
        Assert.Equal(HrAuthResult.NotAuthorisedMessage, result.Message);
        Assert.Null(result.Employee);
    }

    [Fact]
    public async Task AC5_OtherRoleWithWrongEmail_ReturnsInvalidCredentials_NotTheRole()
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync("wrong@company.com", "500100");

        Assert.Equal(HrAuthOutcome.InvalidCredentials, result.Outcome);
    }

    // ---------- AC6: empty fields ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AC6_EmptyEmail_ReportsEmailRequired_WithoutCallingHr(string? email)
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync(email, "100101");

        Assert.Equal(HrAuthOutcome.ValidationFailed, result.Outcome);
        Assert.Equal("Email is required.", result.EmailError);
        Assert.Null(result.EmployeeIdError);
        Assert.Empty(hr.Requests);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AC6_EmptyEmployeeId_ReportsIdRequired_WithoutCallingHr(string? id)
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", id);

        Assert.Equal(HrAuthOutcome.ValidationFailed, result.Outcome);
        Assert.Equal("Employee ID is required.", result.EmployeeIdError);
        Assert.Null(result.EmailError);
        Assert.Empty(hr.Requests);
    }

    [Fact]
    public async Task AC6_BothFieldsEmpty_ReportsBothErrors_WithoutCallingHr()
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync(null, "");

        Assert.Equal(HrAuthOutcome.ValidationFailed, result.Outcome);
        Assert.Equal("Email is required.", result.EmailError);
        Assert.Equal("Employee ID is required.", result.EmployeeIdError);
        Assert.Equal(HrAuthResult.ValidationFailedMessage, result.Message);
        Assert.Empty(hr.Requests);
    }

    // ---------- AC7: data type and format ----------

    [Theory]
    [InlineData("abc")]
    [InlineData("abc@")]
    [InlineData("@company.com")]
    [InlineData("name@company")]                         // no dot in the domain
    [InlineData("a b@company.com")]                      // space inside
    [InlineData("a@b@company.com")]                      // two @ signs
    [InlineData("Sarah <s.mitchell@company.com>")]       // display-name form is not a plain address
    public async Task AC7_InvalidEmailFormat_ReportsFormatError_WithoutCallingHr(string email)
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync(email, "100101");

        Assert.Equal(HrAuthOutcome.ValidationFailed, result.Outcome);
        Assert.NotNull(result.EmailError);
        Assert.NotEqual("Email is required.", result.EmailError);
        Assert.Null(result.EmployeeIdError);
        Assert.Empty(hr.Requests);
    }

    [Fact]
    public async Task AC7_EmailLongerThan254Characters_IsRejected_WithoutCallingHr()
    {
        var hr = new FakeHr();
        var tooLong = new string('a', 250) + "@x.com"; // 256 characters

        var result = await hr.Service.AuthenticateAsync(tooLong, "100101");

        Assert.Equal(HrAuthOutcome.ValidationFailed, result.Outcome);
        Assert.NotNull(result.EmailError);
        Assert.Empty(hr.Requests);
    }

    [Theory]
    [InlineData("ABC12")]
    [InlineData("10.5")]
    [InlineData("-5")]
    [InlineData("+5")]
    [InlineData("0")]
    [InlineData("00")]
    [InlineData("12 34")]
    [InlineData("1,000")]
    [InlineData("2147483648")]                // one more than int.MaxValue
    [InlineData("99999999999999999999")]
    [InlineData("１２３")]                     // full-width digits are not accepted
    public async Task AC7_EmployeeIdThatIsNotAPositiveInt_ReportsFormatError_WithoutCallingHr(string id)
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", id);

        Assert.Equal(HrAuthOutcome.ValidationFailed, result.Outcome);
        Assert.NotNull(result.EmployeeIdError);
        Assert.NotEqual("Employee ID is required.", result.EmployeeIdError);
        Assert.Null(result.EmailError);
        Assert.Empty(hr.Requests);
    }

    [Fact]
    public async Task AC7_BothFieldsInvalid_ReportsBothErrors()
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync("abc", "ABC12");

        Assert.Equal(HrAuthOutcome.ValidationFailed, result.Outcome);
        Assert.NotNull(result.EmailError);
        Assert.NotNull(result.EmployeeIdError);
        Assert.Empty(hr.Requests);
    }

    // ---------- Boundary values for the Employee ID ----------

    [Theory]
    [InlineData("1", "/api/employees/1")]                       // smallest valid ID
    [InlineData("2147483647", "/api/employees/2147483647")]     // int.MaxValue, the largest valid ID
    public async Task Boundary_SmallestAndLargestValidId_AreSentToHr(string id, string expectedPath)
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", id);

        Assert.Equal(HrAuthOutcome.InvalidCredentials, result.Outcome); // valid format, but no such employee in HR
        Assert.Equal(expectedPath, Assert.Single(hr.Requests).RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Boundary_LeadingZerosInId_AreAcceptedAndSentWithoutThem()
    {
        var hr = new FakeHr();

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", "000100101");

        Assert.True(result.IsSuccess);
        Assert.Equal("/api/employees/100101", Assert.Single(hr.Requests).RequestUri!.AbsolutePath);
    }

    // ---------- AC8: HR system problems ----------

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task AC8_UnexpectedStatusCode_ReturnsServiceUnavailable(HttpStatusCode status)
    {
        var hr = new FakeHr(_ => new HttpResponseMessage(status));

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", "100101");

        AssertServiceUnavailable(result);
    }

    [Fact]
    public async Task AC8_HrSystemNotRunning_ReturnsServiceUnavailable()
    {
        var hr = new FakeHr(_ => throw new HttpRequestException("Connection refused"));

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", "100101");

        AssertServiceUnavailable(result);
    }

    [Fact]
    public async Task AC8_HrSystemTimesOut_ReturnsServiceUnavailable()
    {
        var hr = new FakeHr(_ => throw new TaskCanceledException("The request timed out."));

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", "100101");

        AssertServiceUnavailable(result);
    }

    [Fact]
    public async Task AC8_MalformedJson_ReturnsServiceUnavailable()
    {
        var hr = new FakeHr(_ => JsonResponse(HttpStatusCode.OK, "{ this is not json"));

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", "100101");

        AssertServiceUnavailable(result);
    }

    [Fact]
    public async Task AC8_NullJsonBody_ReturnsServiceUnavailable()
    {
        var hr = new FakeHr(_ => JsonResponse(HttpStatusCode.OK, "null"));

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", "100101");

        AssertServiceUnavailable(result);
    }

    [Fact]
    public async Task AC8_NonJsonResponse_ReturnsServiceUnavailable()
    {
        var hr = new FakeHr(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>Proxy error</html>", Encoding.UTF8, "text/html")
        });

        var result = await hr.Service.AuthenticateAsync("s.mitchell@company.com", "100101");

        AssertServiceUnavailable(result);
    }

    [Fact]
    public async Task AC8_RecordForADifferentEmployee_ReturnsServiceUnavailable()
    {
        // The service asked for 100101 but HR answered with employee 200100: never sign in on that.
        var hr = new FakeHr(_ => JsonResponse(HttpStatusCode.OK, FakeHr.EmployeeJson(FakeHr.Employees[200100])));

        var result = await hr.Service.AuthenticateAsync("j.cooper@company.com", "100101");

        AssertServiceUnavailable(result);
    }

    [Fact]
    public async Task AC8_CallerCancellation_IsNotHiddenAsServiceUnavailable()
    {
        var hr = new FakeHr();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => hr.Service.AuthenticateAsync("s.mitchell@company.com", "100101", cancelled.Token));
    }

    [Fact]
    public void Constructor_NullHttpClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HrAuthService(null!));
    }

    // ---------- Helpers ----------

    private static void AssertServiceUnavailable(HrAuthResult result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(HrAuthOutcome.ServiceUnavailable, result.Outcome);
        Assert.Equal(HrAuthResult.ServiceUnavailableMessage, result.Message);
        Assert.Null(result.Employee);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>
    /// A stand-in for the HR system. By default it behaves like <c>GET /api/employees/{id}</c>: 200 with the employee's JSON,
    /// or 404 with a message. Pass a custom responder to simulate failures. Every request is recorded in <see cref="Requests"/>.
    /// </summary>
    private sealed class FakeHr
    {
        /// <summary>The employees used by the tests, copied from the real HR database.</summary>
        public static readonly IReadOnlyDictionary<int, HrEmployeeDto> Employees = new Dictionary<int, HrEmployeeDto>
        {
            [100101] = Make(100101, "Sarah", "Mitchell", "s.mitchell@company.com", HrRoles.BaggageHandler),
            [200100] = Make(200100, "James", "Cooper", "j.cooper@company.com", HrRoles.BaggageSupervisor),
            [300100] = Make(300100, "Priya", "Sharma", "p.sharma@company.com", HrRoles.BaggageHandler),
            [500100] = Make(500100, "Emma", "Johnson", "e.johnson@company.com", "Developer"),
            [700100] = Make(700100, "Olivia", "Brown", "o.brown@company.com", "HR Manager"),
            [900100] = Make(900100, "Emi", "Anderson", "e.anderson@company.com", "Finance Manager")
        };

        private readonly RecordingHandler _handler;

        public FakeHr(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
        {
            _handler = new RecordingHandler(responder ?? Default);
            Service = new HrAuthService(new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:5001/") });
        }

        public HrAuthService Service { get; }

        public IReadOnlyList<HttpRequestMessage> Requests => _handler.Requests;

        public static string EmployeeJson(HrEmployeeDto e) => JsonSerializer.Serialize(new
        {
            employeeId = e.EmployeeId,
            firstName = e.FirstName,
            lastName = e.LastName,
            email = e.Email,
            phone = "07 3001 1001",
            jobTitle = e.JobTitle,
            hireDate = "2019-03-15T00:00:00",
            departmentId = 1,
            departmentName = "Operations"
        });

        private static HttpResponseMessage Default(HttpRequestMessage request)
        {
            var last = request.RequestUri!.Segments[^1];
            return int.TryParse(last, out var id) && Employees.TryGetValue(id, out var employee)
                ? JsonResponse(HttpStatusCode.OK, EmployeeJson(employee))
                : JsonResponse(HttpStatusCode.NotFound, $"{{\"message\":\"Employee with ID {last} was not found.\"}}");
        }

        private static HrEmployeeDto Make(int id, string first, string last, string email, string title) =>
            new() { EmployeeId = id, FirstName = first, LastName = last, Email = email, JobTitle = title };
    }

    /// <summary>An <see cref="HttpMessageHandler"/> that records requests and answers with a supplied function.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        private readonly List<HttpRequestMessage> _requests = new();

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        public IReadOnlyList<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _requests.Add(request);
            return Task.FromResult(_responder(request));
        }
    }
}