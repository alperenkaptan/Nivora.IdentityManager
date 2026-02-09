using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;
using Nivora.IdentityManager.Auth;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages;

public class RegisterModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;
    private readonly IIdentityAdminService _admin;

    public RegisterModel(INivoraIdentityFacade facade, IIdentityAdminService admin)
    {
        _facade = facade;
        _admin = admin;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        // SECURITY FIX #1: Input validation
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Email and password are required.";
            return RedirectToPage();
        }

        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            var response = await _facade.RegisterAsync(new RegisterRequest(email, password), ctx);

            // SECURITY FIX #2: User null check
            var user = await _admin.FindByEmailAsync(email);
            if (user is null)
            {
                ErrorMessage = "Registration completed but user lookup failed. Please log in.";
                return RedirectToPage("/Login");
            }

            await CookieSignInHelper.SignInAsync(HttpContext, user.Id, email);
            AuthSessionStore.SetRefreshToken(HttpContext.Session, response.RefreshToken);
            return RedirectToPage("/Profile");
        }
        catch (IdentityOperationException)
        {
            // SECURITY FIX #3: Error message sanitization (no enumeration)
            ErrorMessage = "Registration failed. This email may already be registered.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred during registration. Please try again.";
            return RedirectToPage();
        }
    }
}