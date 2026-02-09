using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;
using Nivora.IdentityManager.Auth;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages;

public class ExternalLoginModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;

    public ExternalLoginModel(INivoraIdentityFacade facade)
    {
        _facade = facade;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string provider, string providerUserId, string? email)
    {
        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            var response = await _facade.ExternalLoginAsync(
                new ExternalLoginRequest(provider, providerUserId, email), ctx);
            await CookieSignInHelper.SignInFromJwtAsync(HttpContext, response.AccessToken);
            AuthSessionStore.SetRefreshToken(HttpContext.Session, response.RefreshToken);
            return RedirectToPage("/Profile");
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
            return RedirectToPage();
        }
    }
}
