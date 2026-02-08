using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages;

public class LoginModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;

    public LoginModel(INivoraIdentityFacade facade)
    {
        _facade = facade;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            var result = await _facade.LoginAsync(new LoginRequest(email, password), ctx);

            if (result is LoginSuccess success)
            {
                AuthSessionStore.SetTokens(HttpContext.Session,
                    success.Tokens.AccessToken, success.Tokens.RefreshToken);
                return RedirectToPage("/Profile");
            }

            if (result is LoginTwoFactorRequired twoFactor)
            {
                AuthSessionStore.SetChallengeToken(HttpContext.Session, twoFactor.ChallengeToken);
                return RedirectToPage("/Login2fa");
            }

            ErrorMessage = "Unexpected login result.";
            return RedirectToPage();
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
            return RedirectToPage();
        }
    }
}
