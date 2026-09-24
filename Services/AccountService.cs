using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;

namespace gift_of_the_givers.Services;

public class AccountService
{
    private const string DonorRole = "Donor";
    private const string EmployeeRole = "Employee";

    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public AccountService(
        ApplicationDbContext db,
        IHttpContextAccessor httpContextAccessor,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _passwordHasher = passwordHasher;
    }

    // Resolves the current request user so Razor Pages can remain thin and avoid repeating claim parsing.
    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        try
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId is null ? null : await _db.FindUserByIdAsync(userId);
        }
        catch (SqlException)
        {
            return null;
        }
    }

    // Returns the active user's role names so pages can make simple authorization decisions without extra SQL.
    public async Task<IReadOnlyList<string>> GetCurrentUserRolesAsync()
    {
        try
        {
            var user = await GetCurrentUserAsync();
            return user is null ? Array.Empty<string>() : await _db.GetRolesForUserAsync(user.Id);
        }
        catch (SqlException)
        {
            return Array.Empty<string>();
        }
    }

    // Registers a donor account and stores first name and last name separately, which is what the new form collects.
    public async Task<OperationResult<ApplicationUser>> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        try
        {
            if (await _db.UserExistsByEmailAsync(email))
            {
                return new(false, null, "That email address is already registered.");
            }

            var trimmedEmail = email.Trim();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = trimmedEmail,
                NormalizedUserName = trimmedEmail.ToUpperInvariant(),
                Email = trimmedEmail,
                NormalizedEmail = trimmedEmail.ToUpperInvariant(),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                CreatedUtc = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            await _db.CreateUserAsync(user);
            await _db.AddUserToRoleAsync(user.Id, DonorRole);

            return new(true, user, null);
        }
        catch (SqlException)
        {
            return new(false, null, "The database connection is unavailable right now. Please try again after SQL Server is started.");
        }
    }

    // Validates an email/password pair against the stored password hash.
    public async Task<OperationResult<ApplicationUser>> LoginAsync(string email, string password)
    {
        try
        {
            var user = await _db.FindUserByEmailAsync(email);
            if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return new(false, null, "Invalid login attempt.");
            }

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return new(false, null, "Invalid login attempt.");
            }

            return new(true, user, null);
        }
        catch (SqlException)
        {
            return new(false, null, "The database connection is unavailable right now. Please try again after SQL Server is started.");
        }
    }

    // Creates the cookie principal so the request pipeline can treat the user as signed in on the next request.
    public async Task SignInAsync(ApplicationUser user, bool isPersistent)
    {
        var httpContext = GetHttpContext();
        IReadOnlyList<string> roles;
        try
        {
            roles = await _db.GetRolesForUserAsync(user.Id);
        }
        catch (SqlException)
        {
            roles = Array.Empty<string>();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? "Member" : user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("first_name", user.FirstName),
            new("last_name", user.LastName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                AllowRefresh = true,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(14) : null
            });
    }

    // Signs the active user out by clearing the authentication cookie.
    public async Task SignOutAsync()
    {
        await GetHttpContext().SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    // Creates a SQL-backed user record and assigns one or more roles.
    public async Task<ApplicationUser> CreateUserAsync(ApplicationUser user, string password, params string[] roles)
    {
        try
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            user.EmailConfirmed = true;
            user.NormalizedEmail = user.Email?.Trim().ToUpperInvariant();
            user.NormalizedUserName = user.UserName?.Trim().ToUpperInvariant();
            user.CreatedUtc = DateTime.UtcNow;

            await _db.CreateUserAsync(user);
            if (roles.Length == 0)
            {
                roles = new[] { DonorRole };
            }

            foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await _db.AddUserToRoleAsync(user.Id, role);
            }

            return user;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("The database connection is unavailable right now.", ex);
        }
    }

    // Ensures the two roles used by the prototype exist before the seed data runs.
    public async Task EnsureSeedRolesAsync()
    {
        try
        {
            await _db.EnsureRoleAsync(DonorRole);
            await _db.EnsureRoleAsync(EmployeeRole);
        }
        catch (SqlException)
        {
        }
    }

    // Pulls the request context from the accessor and stops early if the service is called outside a request.
    private HttpContext GetHttpContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new InvalidOperationException("No active HTTP context is available.");
        }

        return httpContext;
    }
}
