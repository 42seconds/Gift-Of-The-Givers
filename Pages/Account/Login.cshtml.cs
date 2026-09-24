using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using gift_of_the_givers.Services;

namespace gift_of_the_givers.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly AccountService _accountService;

        public LoginModel(AccountService accountService)
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
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        // Loads the login screen without any extra server work because the form itself is the main interaction.
        public void OnGet()
        {
        }

        // Validates credentials, signs the user in, and redirects them to the original local return path when possible.
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl = GetSafeReturnUrl(returnUrl ?? ReturnUrl);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _accountService.LoginAsync(Input.Email, Input.Password);
            if (!result.Succeeded || result.Value is null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Invalid login attempt.");
                return Page();
            }

            await _accountService.SignInAsync(result.Value, Input.RememberMe);
            TempData["Success"] = $"Welcome back, {result.Value.DisplayName}.";
            return LocalRedirect(returnUrl);
        }

        // Prevents open redirects by allowing only local URLs and falling back to the home page.
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
