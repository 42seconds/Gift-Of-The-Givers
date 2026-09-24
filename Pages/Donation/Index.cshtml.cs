using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using gift_of_the_givers.Services;

namespace gift_of_the_givers.Pages.Donation
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly AccountService _accountService;

        public IndexModel(ApplicationDbContext db, AccountService accountService)
        {
            _db = db;
            _accountService = accountService;
        }

        [BindProperty]
        public global::gift_of_the_givers.Models.Donation Donation { get; set; } = new();

        [BindProperty]
        public bool DonateAsGuest { get; set; }

        // Pre-fills the form with the logged-in donor's details so the page feels lighter for returning users.
        public async Task OnGetAsync()
        {
            Donation = new global::gift_of_the_givers.Models.Donation();

            var user = await _accountService.GetCurrentUserAsync();
            if (user is not null)
            {
                Donation.DonorName = user.DisplayName;
                Donation.DonorEmail = user.Email;
            }
        }

        // Captures the donation, writes it to SQL, and routes the user to a basic thank-you result.
        // If the database is unreachable, we keep the form on screen and explain the failure instead of exposing the raw SQL exception.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _accountService.GetCurrentUserAsync();

            var wantsAnonymous = DonateAsGuest || Donation.IsAnonymous;

            if (user != null && !wantsAnonymous)
            {
                Donation.UserId = user.Id;
                Donation.DonorName = user.DisplayName;
                Donation.DonorEmail = user.Email;
                Donation.IsAnonymous = false;
            }
            else
            {
                Donation.IsAnonymous = true;
                Donation.DonorName = string.IsNullOrWhiteSpace(Donation.DonorName) ? "Anonymous Donor" : Donation.DonorName;
                if (string.IsNullOrWhiteSpace(Donation.DonorEmail))
                {
                    Donation.DonorEmail = null;
                }
            }

            Donation.DonationDate = DateTime.UtcNow;

            try
            {
                await _db.InsertDonationAsync(Donation);
                TempData["Success"] = "Donation recorded successfully.";
                return RedirectToPage("/Donation/Certificate", new { id = Donation.Id });
            }
            catch (SqlException)
            {
                TempData["Error"] = "We could not save the donation because the database connection is unavailable right now.";
                return Page();
            }
        }
    }
}
