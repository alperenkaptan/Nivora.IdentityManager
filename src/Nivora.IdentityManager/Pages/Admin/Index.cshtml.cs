using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly IIdentityAdminService _admin;

    public IndexModel(IIdentityAdminService admin)
    {
        _admin = admin;
    }

    public bool IsForbidden { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await IsAdminAsync())
        {
            IsForbidden = true;
            Response.StatusCode = 403;
            return Page();
        }

        return Page();
    }

    private async Task<bool> IsAdminAsync()
    {
        var userId = AuthSessionStore.GetUserId(HttpContext.Session);
        if (userId is null) return false;
        var roles = await _admin.GetUserRolesAsync(userId.Value);
        return roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
    }
}
