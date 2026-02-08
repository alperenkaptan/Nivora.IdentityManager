using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages;

public class Login2faModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;

    public Login2faModel(INivoraIdentityFacade facade)
    {
        _facade = facade;
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
            AuthSessionStore.ClearChallenge(HttpContext.Session);
            AuthSessionStore.SetTokens(HttpContext.Session, response.AccessToken, response.RefreshToken);
            return RedirectToPage("/Profile");
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
            return RedirectToPage();
        }
    }
}
