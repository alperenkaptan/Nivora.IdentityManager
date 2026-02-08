using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;

namespace Nivora.IdentityManager.Pages;

public class ForgotPasswordModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;

    public ForgotPasswordModel(INivoraIdentityFacade facade)
    {
        _facade = facade;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email)
    {
        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            await _facade.ForgotPasswordAsync(new ForgotPasswordRequest(email), ctx);
        }
        catch
        {
            // Swallow — never reveal whether email exists
        }

        StatusMessage = "If an account exists, password reset instructions were sent. An admin can generate the token from the Admin panel.";
        return RedirectToPage();
    }
}
