using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GiftOfTheGivers.Pages
{
    public class DonateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DonateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [BindProperty]
        public Donation Donation { get; set; } = new();

        [BindProperty]
        public bool DonateAsGuest { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.GetUserAsync(User);

            if (user != null && !DonateAsGuest)
            {
                Donation.UserId = user.Id;
                Donation.DonorName = user.FullName ?? user.Email ?? "Registered Donor";
                Donation.DonorEmail = user.Email;
                Donation.IsAnonymous = false;
            }
            else
            {
                Donation.IsAnonymous = string.IsNullOrWhiteSpace(Donation.DonorName);
                Donation.DonorName = string.IsNullOrWhiteSpace(Donation.DonorName) ? "Anonymous Donor" : Donation.DonorName;
            }

            Donation.DonationDate = DateTime.UtcNow;
            Donation.CertificateReference = $"GOTG-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(100000, 999999)}";

            _db.Donations.Add(Donation);
            await _db.SaveChangesAsync();

            return RedirectToPage("DonateThankYou", new { id = Donation.Id });
        }
    }
}
