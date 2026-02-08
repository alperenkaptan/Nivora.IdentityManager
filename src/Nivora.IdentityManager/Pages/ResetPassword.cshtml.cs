using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;

namespace Nivora.IdentityManager.Pages;

public class ResetPasswordModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;

    public ResetPasswordModel(INivoraIdentityFacade facade)
    {
        _facade = facade;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string token, string newPassword)
    {
        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            await _facade.ResetPasswordAsync(new ResetPasswordRequest(token, newPassword), ctx);
            StatusMessage = "Password has been reset. You can now log in.";
            return RedirectToPage("/Login");
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
            return RedirectToPage();
        }
    }
}
