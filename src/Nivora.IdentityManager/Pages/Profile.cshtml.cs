using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages;

public class ProfileModel : PageModel
{
    private readonly IdentityApiClient _api;

    public ProfileModel(IdentityApiClient api) => _api = api;

    public MeInfo? Me { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var result = await _api.GetMeWithRefreshAsync(HttpContext.Session);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        Me = result.Data;
        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        var refreshToken = AuthSessionStore.GetRefreshToken(HttpContext.Session);
        if (!string.IsNullOrEmpty(refreshToken))
            await _api.LogoutAsync(refreshToken);

        AuthSessionStore.Clear(HttpContext.Session);
        return RedirectToPage("/Login");
    }
}
