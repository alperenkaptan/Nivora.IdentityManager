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
    private readonly IIdentityAdminService _admin;

    public ExternalLoginModel(INivoraIdentityFacade facade, IIdentityAdminService admin)
    {
        _facade = facade;
        _admin = admin;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string provider, string providerUserId, string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = "Email is required for external login.";
            return RedirectToPage();
        }

        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            var response = await _facade.ExternalLoginAsync(
                new ExternalLoginRequest(provider, providerUserId, email), ctx);

            var user = await _admin.FindByEmailAsync(email);
            await CookieSignInHelper.SignInAsync(HttpContext, user!.Id, email);
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
