using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;
using Nivora.IdentityManager.Auth;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages;

public class LoginModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;
    private readonly IIdentityAdminService _admin;

    public LoginModel(INivoraIdentityFacade facade, IIdentityAdminService admin)
    {
        _facade = facade;
        _admin = admin;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Email and password are required.";
            return RedirectToPage();
        }

        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            var result = await _facade.LoginAsync(new LoginRequest(email, password), ctx);

            if (result is LoginSuccess success)
            {
                var user = await _admin.FindByEmailAsync(email);
                if (user is null)
                {
                    ErrorMessage = "Invalid credentials. Please try again.";
                    return RedirectToPage();
                }

                await CookieSignInHelper.SignInAsync(HttpContext, user.Id, email);
                AuthSessionStore.SetRefreshToken(HttpContext.Session, success.Tokens.RefreshToken);
                return RedirectToPage("/Profile");
            }

            if (result is LoginTwoFactorRequired twoFactor)
            {
                AuthSessionStore.SetChallengeToken(HttpContext.Session, twoFactor.ChallengeToken);
                AuthSessionStore.SetLoginEmail(HttpContext.Session, email);
                return RedirectToPage("/Login2fa");
            }

            ErrorMessage = "Invalid credentials. Please try again.";
            return RedirectToPage();
        }
        catch (IdentityOperationException)
        {
            // Don't expose internal details
            ErrorMessage = "Invalid credentials. Please try again.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred during login. Please try again.";
            return RedirectToPage();
        }
    }
}
