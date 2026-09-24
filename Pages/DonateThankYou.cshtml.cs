using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GiftOfTheGivers.Pages
{
    public class DonateThankYouModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DonateThankYouModel(ApplicationDbContext db) => _db = db;

        public Donation Donation { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var donation = await _db.Donations.FindAsync(id);
            if (donation is null) return NotFound();
            Donation = donation;
            return Page();
        }
    }
}
