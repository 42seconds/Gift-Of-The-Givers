using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GiftOfTheGivers.Pages
{
    public class VolunteerModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public VolunteerModel(ApplicationDbContext db) => _db = db;

        [BindProperty]
        public VolunteerSignup Volunteer { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            Volunteer.SubmittedOn = DateTime.UtcNow;
            _db.VolunteerSignups.Add(Volunteer);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Thank you! Your volunteer application has been received. " +
                                   "Our team will be in touch regarding next steps.";
            return RedirectToPage();
        }
    }
}
