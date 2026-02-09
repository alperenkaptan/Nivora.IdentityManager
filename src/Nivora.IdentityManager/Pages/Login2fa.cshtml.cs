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
        var challenge = AuthSessionStore.GetChallengeToken(HttpContext.Session);
        if (string.IsNullOrEmpty(challenge))
            return RedirectToPage("/Login");

        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            var response = await _facade.Complete2FaLoginAsync(new Login2FaRequest(challenge, code), ctx);

            var email = AuthSessionStore.GetLoginEmail(HttpContext.Session);
            AuthSessionStore.ClearChallenge(HttpContext.Session);

            var user = await _admin.FindByEmailAsync(email!);
            await CookieSignInHelper.SignInAsync(HttpContext, user!.Id, email!);
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
