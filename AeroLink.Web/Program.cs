using AeroLink.Web.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AeroLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AeroLink")));

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

app.UseAuthorization();

app.MapRazorPages();

app.Run();
