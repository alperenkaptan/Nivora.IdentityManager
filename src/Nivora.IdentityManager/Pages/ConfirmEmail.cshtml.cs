using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;

namespace Nivora.IdentityManager.Pages;

public class ConfirmEmailModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;

    public ConfirmEmailModel(INivoraIdentityFacade facade)
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

    public async Task<IActionResult> OnPostAsync(string token)
    {
        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            await _facade.ConfirmEmailAsync(new ConfirmEmailRequest(token), ctx);
            StatusMessage = "Email confirmed successfully.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }
}
