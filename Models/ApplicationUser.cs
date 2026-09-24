namespace gift_of_the_givers.Models;

public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // The UI and cookie claims use this helper so the app can store names separately but still display one label.
    public string DisplayName => string.Join(' ', new[] { FirstName, LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
