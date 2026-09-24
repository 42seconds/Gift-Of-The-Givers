using Microsoft.AspNetCore.Identity;

namespace GiftOfTheGivers.Models
{
    // Extends the default Identity user with a couple of extra profile fields.
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Department { get; set; } // only meaningful for Employee role
    }
}
