using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using gift_of_the_givers.Services;

namespace gift_of_the_givers.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly AccountService _accountService;

        public LogoutModel(AccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        // Shows a small confirmation page so the sign-out action stays a deliberate POST request.
        public void OnGet()
        {
        }

        // Clears the authentication cookie and sends the user back to a local destination.
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            await _accountService.SignOutAsync();
            TempData["Success"] = "You have been signed out.";
            return LocalRedirect(GetSafeReturnUrl(returnUrl ?? ReturnUrl));
        }

        // Keeps logout redirects local so the app does not hand control to an external URL.
        private string GetSafeReturnUrl(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Page("/Index") ?? "/";
        }
    }
}
