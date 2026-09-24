using System.ComponentModel.DataAnnotations;

namespace gift_of_the_givers.Models;

public class VolunteerSignup
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required, StringLength(300)]
    public string Skills { get; set; } = string.Empty;

    [Required]
    public string Availability { get; set; } = string.Empty;

    public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;
}
