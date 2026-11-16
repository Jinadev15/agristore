using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgriStore.Web.Areas.Identity.Pages.Account;

/// <summary>Our own registration page. New accounts are staff (not admin).</summary>
[AllowAnonymous]
public class RegisterModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; private set; } = "/Dashboard";

    public class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";

        [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Use at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect("/Dashboard");
        ReturnUrl = SafeReturnUrl(returnUrl);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        if (!ModelState.IsValid) return Page();

        var email = Input.Email.Trim();
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: false);
            TempData["Message"] = "Account created. Welcome to AgriStore.";
            return LocalRedirect(ReturnUrl);
        }

        foreach (var error in result.Errors)
        {
            var message = error.Code == "DuplicateUserName" ? "An account with this email already exists. Sign in instead." : error.Description;
            ModelState.AddModelError(error.Code.StartsWith("Password") ? "Input.Password" : string.Empty, message);
        }
        return Page();
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" ? returnUrl : "/Dashboard";
}
