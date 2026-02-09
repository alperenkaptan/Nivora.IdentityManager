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

    public RegisterModel(INivoraIdentityFacade facade)
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
            var response = await _facade.RegisterAsync(new RegisterRequest(email, password), ctx);
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
