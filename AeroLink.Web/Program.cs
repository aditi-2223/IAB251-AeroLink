using AeroLink.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AeroLink.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // T1: everything under /Baggage and the main screen require sign-in.
    options.Conventions.AuthorizeFolder("/Baggage");
    options.Conventions.AuthorizePage("/Index");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
});

// T1: cookie authentication. The HR login (T2) calls HttpContext.SignInAsync with this default scheme.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

builder.Services.AddDbContext<AeroLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AeroLink")));

// T2: HR system connection. BaseUrl and TimeoutSeconds come from the "HrApi" section of appsettings.json.
builder.Services.Configure<HrApiOptions>(builder.Configuration.GetSection(HrApiOptions.SectionName));
builder.Services.AddHttpClient<IHrAuthService, HrAuthService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<HrApiOptions>>().Value;
    client.BaseAddress = options.GetBaseUri();
    client.Timeout = options.GetTimeout();
});

builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IBagVerificationService, BagVerificationService>();
builder.Services.AddScoped<IExceptionService, ExceptionService>();
builder.Services.AddSingleton<ISpecialHandlingService, SpecialHandlingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AeroLinkDbContext>();
    var seedPath = Path.Combine(app.Environment.ContentRootPath, "Data", "SeedData",
        "IAB251_Synthetic_Baggage_Manifest.xlsx");
    DbSeeder.Seed(db, seedPath);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // T1: must come BEFORE UseAuthorization
app.UseAuthorization();

app.MapRazorPages();

app.Run();