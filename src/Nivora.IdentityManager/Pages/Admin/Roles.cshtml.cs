using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;

namespace Nivora.IdentityManager.Pages.Admin;

[Authorize(Roles = "Admin")]
public class RolesModel : PageModel
{
    private readonly IIdentityAdminService _admin;

    public RolesModel(IIdentityAdminService admin)
    {
        _admin = admin;
    }

    public List<string> Roles { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Roles = (await _admin.GetAllRolesAsync()).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(string roleName)
    {
        await _admin.CreateRoleAsync(roleName);
        StatusMessage = $"Role '{roleName}' created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string roleName)
    {
        await _admin.DeleteRoleAsync(roleName);
        StatusMessage = $"Role '{roleName}' deleted.";
        return RedirectToPage();
    }
}
