using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using gift_of_the_givers.Services;

namespace gift_of_the_givers.Pages.Employee
{
    [Authorize(Roles = "Employee")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly AccountService _accountService;
        private readonly AzureFunctionsClient _functions;

        public DashboardModel(ApplicationDbContext db, AccountService accountService, AzureFunctionsClient functions)
        {
            _db = db;
            _accountService = accountService;
            _functions = functions;
        }

        [BindProperty]
        public ProjectUpdate ProjectUpdate { get; set; } = new();

        public int DonationCount { get; set; }

        public decimal DonationTotal { get; set; }

        public int VolunteerCount { get; set; }

        public int UpdateCount { get; set; }

        public IList<global::gift_of_the_givers.Models.Donation> RecentDonations { get; set; } = new List<global::gift_of_the_givers.Models.Donation>();

        public IList<VolunteerSignup> RecentVolunteers { get; set; } = new List<VolunteerSignup>();

        public IList<ProjectUpdate> ProjectUpdates { get; set; } = new List<ProjectUpdate>();

        // Latest entries from the Azure Table Storage log (null when the Function App is offline).
        public IReadOnlyList<ProjectUpdateLogEntry>? StorageLog { get; set; }

        // Loads the current dashboard snapshot, including summary metrics and the latest records from each table.
        public async Task OnGetAsync()
        {
            await LoadDashboardDataAsync();
        }

        // Stores a new project update and then reloads the dashboard so the employee can see it immediately.
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(ProjectUpdate.Title) || string.IsNullOrWhiteSpace(ProjectUpdate.Description))
            {
                TempData["Error"] = "Title and description are required.";
                await LoadDashboardDataAsync();
                return Page();
            }

            var user = await _accountService.GetCurrentUserAsync();
            ProjectUpdate.PostedByUserId = user?.Id ?? "unknown";
            ProjectUpdate.PostedByName = user?.DisplayName ?? user?.Email;
            ProjectUpdate.PostedOn = DateTime.UtcNow;

            try
            {
                await _db.InsertProjectUpdateAsync(ProjectUpdate);

                // Hand the update to the LogProjectUpdate Azure Function, which records it in Azure Table Storage.
                var logged = await _functions.LogProjectUpdateAsync(ProjectUpdate);
                TempData["Success"] = logged
                    ? "Project update posted successfully and logged to Azure Storage."
                    : "Project update posted successfully (Azure Function offline, so it was not logged to Azure Storage).";
                return RedirectToPage("/Employee/Dashboard");
            }
            catch (SqlException)
            {
                TempData["Error"] = "We could not post the update because the database connection is unavailable right now.";
                await LoadDashboardDataAsync();
                return Page();
            }
        }

        // Pulls the employee dashboard data in one place so the page model stays easy to follow.
        private async Task LoadDashboardDataAsync()
        {
            StorageLog = await _functions.GetProjectUpdateLogAsync(5);

            DonationCount = RecentDonations.Count;
            DonationTotal = 0m;
            VolunteerCount = 0;
            UpdateCount = 0;

            try
            {
                RecentDonations = (await _db.GetRecentDonationsAsync(10)).ToList();
                RecentVolunteers = (await _db.GetRecentVolunteerSignupsAsync(10)).ToList();
                ProjectUpdates = (await _db.GetAllProjectUpdatesAsync()).ToList();

                DonationCount = RecentDonations.Count;
                DonationTotal = RecentDonations.Sum(d => d.Amount);
                VolunteerCount = RecentVolunteers.Count;
                UpdateCount = ProjectUpdates.Count;
            }
            catch (SqlException)
            {
                RecentDonations = new List<global::gift_of_the_givers.Models.Donation>();
                RecentVolunteers = new List<VolunteerSignup>();
                ProjectUpdates = new List<ProjectUpdate>();

                DonationCount = 0;
                DonationTotal = 0m;
                VolunteerCount = 0;
                UpdateCount = 0;

                TempData["Error"] = "Employee dashboard data is unavailable because the database connection could not be reached.";
            }
        }
    }
}
