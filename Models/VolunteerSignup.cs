using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGivers.Models
{
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
        public string Skills { get; set; } = string.Empty; // e.g. "First Aid, Logistics, Driving"

        [Required]
        public string Availability { get; set; } = string.Empty; // e.g. "Weekends", "Full-time during crisis"

        public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;
    }
}
