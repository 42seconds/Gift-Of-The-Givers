using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// -------------------------------------------------------------------------
// Gift of the Givers — Razor Pages web app
//
// This project is intentionally Razor Pages only (no MVC Controllers/Views).
// Form handling that used to live in Controllers (Donation, Volunteer,
// Employee) now lives in PageModel OnGet/OnPost handlers under /Pages.
// -------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// QuestPDF community license (free for small orgs/non-profits/prototypes).
// Used by Services/TaxCertificateGenerator.cs to produce the donation PDF.
QuestPDF.Settings.License = LicenseType.Community;

// ---- Database ----
// SQLite for local development/prototype use. To point this at Azure SQL
// for deployment, swap the connection string in appsettings.json and change
// UseSqlite(...) to UseSqlServer(...) here.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=gotg.db"));

// ---- ASP.NET Core Identity with role-based access ----
// AddRoles<IdentityRole>() enables the "Donor" / "Employee" roles used
// throughout the app (see [Authorize(Roles = "Employee")] on the Dashboard
// PageModel, and the role checks in Donate.cshtml).
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // relaxed for prototype/coursework use
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Razor Pages is the only UI framework registered — this also covers the
// scaffolded Identity UI pages (Login/Register/etc.), which are Razor Pages
// themselves, shipped inside the Identity.UI package.
builder.Services.AddRazorPages();

var app = builder.Build();

// ---- Seed database, roles, and a demo employee account ----
// Runs once at startup: creates the SQLite file if it doesn't exist yet,
// then ensures the "Donor"/"Employee" roles and a demo Employee login exist
// so markers/testers can log in without manually creating an account.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    await SeedData.InitializeAsync(services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication must run before Authorization so that [Authorize] attributes
// (e.g. on the Employee Dashboard PageModel) have a signed-in user to check.
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
