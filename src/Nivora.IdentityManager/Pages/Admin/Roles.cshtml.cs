using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages.Admin;

public class RolesModel : PageModel
{
    private readonly IIdentityAdminService _admin;

    public RolesModel(IIdentityAdminService admin)
    {
        _admin = admin;
    }

    public List<string> Roles { get; set; } = [];
    public bool IsForbidden { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    private async Task<bool> IsAdminAsync()
    {
        var userId = AuthSessionStore.GetUserId(HttpContext.Session);
        if (userId is null) return false;
        var roles = await _admin.GetUserRolesAsync(userId.Value);
        return roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await IsAdminAsync())
        {
            IsForbidden = true;
            Response.StatusCode = 403;
            return Page();
        }

        Roles = (await _admin.GetAllRolesAsync()).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(string roleName)
    {
        if (!await IsAdminAsync()) return Forbid();

        await _admin.CreateRoleAsync(roleName);
        StatusMessage = $"Role '{roleName}' created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string roleName)
    {
        if (!await IsAdminAsync()) return Forbid();

        await _admin.DeleteRoleAsync(roleName);
        StatusMessage = $"Role '{roleName}' deleted.";
        return RedirectToPage();
    }
}
