using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;

namespace gift_of_the_givers.Pages.Volunteer
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public RegisterModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public VolunteerSignup Signup { get; set; } = new();

        // Displays an empty volunteer form because this page only needs the submission action, not any extra lookup data.
        public void OnGet()
        {
            Signup = new VolunteerSignup();
        }

        // Stores the volunteer application in SQL and returns the user to the same page with a success message.
        // When the SQL connection is not available, we preserve the form data and show a friendly error so the user can try again later.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Signup.SubmittedOn = DateTime.UtcNow;
            try
            {
                await _db.InsertVolunteerSignupAsync(Signup);
                TempData["Success"] = "Thank you! Your volunteer application has been received.";
                return RedirectToPage("/Volunteer/Register");
            }
            catch (SqlException)
            {
                TempData["Error"] = "We could not save the volunteer application because the database connection is unavailable right now.";
                return Page();
            }
        }
    }
}
