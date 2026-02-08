using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.IdentityManager.Data;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages;

public class RegisterModel : PageModel
{
    private readonly IdentityApiClient _api;
    private readonly AppDbContext _db;

    public RegisterModel(IdentityApiClient api, AppDbContext db)
    {
        _api = api;
        _db = db;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        var result = await _api.RegisterAsync(email, password);
        if (!result.Success)
        {
            ErrorMessage = await AuthErrorHelper.EnrichErrorAsync(_db, email, result.Error!);
            return RedirectToPage();
        }

        AuthSessionStore.SetTokens(HttpContext.Session, result.Data!.AccessToken, result.Data.RefreshToken);
        return RedirectToPage("/Profile");
    }
}
