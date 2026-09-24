using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using gift_of_the_givers.Services;

var builder = WebApplication.CreateBuilder(args);

// Keep logging simple so startup warnings do not trip over Windows Event Log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register the small prototype stack: SQL Client data access, cookie auth, and Razor Pages.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ApplicationDbContext>();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddScoped<AccountService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "GiftOfTheGivers.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
    });

builder.Services.AddAuthorization();
builder.Services.AddRazorPages();

var app = builder.Build();

// Create the schema and seed a couple of demo users before requests start flowing through the app.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    var accountService = services.GetRequiredService<AccountService>();

    // Keep the web host alive even if SQL Server is not installed or the local instance is offline.
    try
    {
        await db.InitializeAsync();
        await SeedData.InitializeAsync(db, accountService);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogWarning(ex, "Database initialization was skipped because the SQL connection could not be opened.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
