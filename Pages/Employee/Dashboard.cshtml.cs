using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Pages.Employee
{
    // Only signed-in users in the "Employee" role can reach this page.
    // This is the Part 1 requirement for ASP.NET Identity role-based access —
    // Donors and anonymous visitors are redirected to the login page automatically
    // by the Identity middleware if they try to hit /Employee/Dashboard directly.
    [Authorize(Roles = "Employee")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ---- Data shown on the dashboard (read-only, populated in OnGet) ----
        public List<Donation> RecentDonations { get; set; } = new();
        public List<VolunteerSignup> Volunteers { get; set; } = new();
        public List<ProjectUpdate> Updates { get; set; } = new();

        // ---- Simple KPI figures for the stat cards at the top of the page ----
        // NOTE: donations are kept separated by currency rather than converted
        // to a single total, since the Donation model doesn't store exchange
        // rates and inventing a conversion factor here would be misleading.
        public int TotalDonationCount { get; set; }
        public decimal TotalZar { get; set; }
        public decimal TotalUsd { get; set; }
        public decimal TotalEur { get; set; }
        public int TotalVolunteerCount { get; set; }
        public int TotalUpdateCount { get; set; }

        // Bound to the "Post a Project Update" form below.
        [BindProperty]
        public ProjectUpdate NewUpdate { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDashboardDataAsync();
        }

        // Handles the "Post a Project Update" form submission.
        // Field employees use this to publish situation reports that show
        // up on the dashboard's "Posted Updates" feed.
        public async Task<IActionResult> OnPostAsync()
        {
            // Only validate the two fields the employee actually fills in —
            // PostedByUserId/PostedByName/PostedOn are set below, not by the form,
            // so we don't want ModelState failing on those.
            ModelState.Remove(nameof(NewUpdate.PostedByUserId));

            if (string.IsNullOrWhiteSpace(NewUpdate.Title) || string.IsNullOrWhiteSpace(NewUpdate.Description))
            {
                TempData["Error"] = "Title and description are required.";
                await LoadDashboardDataAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);

            NewUpdate.PostedByUserId = user?.Id ?? "unknown";
            NewUpdate.PostedByName = user?.FullName ?? user?.Email;
            NewUpdate.PostedOn = DateTime.UtcNow;

            _db.ProjectUpdates.Add(NewUpdate);
            await _db.SaveChangesAsync();

            TempData["Notification"] = "Field update published successfully.";

            // Redirect-after-post avoids a duplicate submission if the employee refreshes the page.
            return RedirectToPage();
        }

        // Pulls everything the dashboard needs in one place so both OnGet
        // and the "invalid form" path in OnPost can reuse it without duplicating queries.
        private async Task LoadDashboardDataAsync()
        {
            RecentDonations = await _db.Donations
                .OrderByDescending(d => d.DonationDate)
                .Take(20)
                .ToListAsync();

            Volunteers = await _db.VolunteerSignups
                .OrderByDescending(v => v.SubmittedOn)
                .ToListAsync();

            Updates = await _db.ProjectUpdates
                .OrderByDescending(u => u.PostedOn)
                .ToListAsync();

            TotalDonationCount = await _db.Donations.CountAsync();
            TotalZar = await _db.Donations.Where(d => d.Currency == Currency.ZAR).SumAsync(d => (decimal?)d.Amount) ?? 0;
            TotalUsd = await _db.Donations.Where(d => d.Currency == Currency.USD).SumAsync(d => (decimal?)d.Amount) ?? 0;
            TotalEur = await _db.Donations.Where(d => d.Currency == Currency.EUR).SumAsync(d => (decimal?)d.Amount) ?? 0;

            TotalVolunteerCount = await _db.VolunteerSignups.CountAsync();
            TotalUpdateCount = await _db.ProjectUpdates.CountAsync();
        }
    }
}
