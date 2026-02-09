using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;
using Nivora.IdentityManager.Auth;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages;

public class Login2faModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;
    private readonly IIdentityAdminService _admin;

    public Login2faModel(INivoraIdentityFacade facade, IIdentityAdminService admin)
    {
        _facade = facade;
        _admin = admin;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        var challenge = AuthSessionStore.GetChallengeToken(HttpContext.Session);
        if (string.IsNullOrEmpty(challenge))
            return RedirectToPage("/Login");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string code)
    {
        // SECURITY FIX #1: Input validation
        if (string.IsNullOrWhiteSpace(code))
        {
            ErrorMessage = "2FA code is required.";
            return RedirectToPage();
        }

        var challenge = AuthSessionStore.GetChallengeToken(HttpContext.Session);
        if (string.IsNullOrEmpty(challenge))
            return RedirectToPage("/Login");

        // SECURITY FIX #2: Challenge token expiry check
        if (AuthSessionStore.IsChallengeExpired(HttpContext.Session))
        {
            ErrorMessage = "2FA challenge expired. Please log in again.";
            return RedirectToPage("/Login");
        }

        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            var response = await _facade.Complete2FaLoginAsync(new Login2FaRequest(challenge, code), ctx);

            var email = AuthSessionStore.GetLoginEmail(HttpContext.Session);
            AuthSessionStore.ClearChallenge(HttpContext.Session);

            var user = await _admin.FindByEmailAsync(email!);
            if (user is null)
            {
                ErrorMessage = "User not found. Please log in again.";
                return RedirectToPage("/Login");
            }

            await CookieSignInHelper.SignInAsync(HttpContext, user.Id, email!);
            AuthSessionStore.SetRefreshToken(HttpContext.Session, response.RefreshToken);
            return RedirectToPage("/Profile");
        }
        catch (IdentityOperationException)
        {
            ErrorMessage = "Invalid 2FA code. Please try again.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred. Please try again.";
            return RedirectToPage();
        }
    }
}
