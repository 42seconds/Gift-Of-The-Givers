using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGivers.Models
{
    public class ProjectUpdate
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty; // e.g. "KZN Flood Relief - Phase 2"

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string PostedByUserId { get; set; } = string.Empty;

        public string? PostedByName { get; set; }

        public DateTime PostedOn { get; set; } = DateTime.UtcNow;
    }
}
