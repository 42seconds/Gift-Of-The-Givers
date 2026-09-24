using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using gift_of_the_givers.Services;

namespace gift_of_the_givers.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly AccountService _accountService;

        public RegisterModel(AccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [StringLength(100)]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [StringLength(100)]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Renders the empty registration form; the heavy lifting happens on the post handler.
        public void OnGet()
        {
        }

        // Creates the user, signs them in immediately, and redirects back to a safe local URL.
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl = GetSafeReturnUrl(returnUrl ?? ReturnUrl);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _accountService.RegisterAsync(Input.FirstName, Input.LastName, Input.Email, Input.Password);
            if (!result.Succeeded || result.Value is null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
                return Page();
            }

            await _accountService.SignInAsync(result.Value, false);
            TempData["Success"] = $"Welcome, {result.Value.DisplayName}. Your account is ready.";
            return LocalRedirect(returnUrl);
        }

        // Keeps redirect targets local so the registration flow cannot bounce to an external site.
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
