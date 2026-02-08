using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nivora.Identity.Abstractions;
using Nivora.IdentityManager.Data;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages.Admin;

public class UsersModel : PageModel
{
    private readonly IIdentityAdminService _admin;
    private readonly AppDbContext _db;

    public UsersModel(IIdentityAdminService admin, AppDbContext db)
    {
        _admin = admin;
        _db = db;
    }

    public List<IdentityUserRow> Users { get; set; } = [];
    public Dictionary<Guid, List<string>> UserRoles { get; set; } = [];
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

        Users = await _db.Set<IdentityUserRow>()
            .FromSqlRaw(
                "SELECT Id, Email, NormalizedEmail, IsDisabled, AccessFailedCount, LockoutEnd, CreatedAt, LastLoginAt, EmailConfirmedAt, PhoneNumber, PhoneConfirmedAt, TwoFactorEnabled FROM Users ORDER BY CreatedAt DESC")
            .ToListAsync();

        // Her user için roles'? fetch et
        foreach (var user in Users)
        {
            UserRoles[user.Id] = (await _admin.GetUserRolesAsync(user.Id)).ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync(Guid userId)
    {
        if (!await IsAdminAsync()) return Forbid();

        await _admin.DisableUserAsync(userId, "admin panel");
        StatusMessage = "User disabled.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEnableAsync(Guid userId)
    {
        if (!await IsAdminAsync()) return Forbid();

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE Users SET IsDisabled = 0 WHERE Id = {0}", userId);
        StatusMessage = "User enabled.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid userId)
    {
        if (!await IsAdminAsync()) return Forbid();

        await _admin.RevokeAllSessionsAsync(userId, "admin panel");
        StatusMessage = "All sessions revoked.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetPasswordAsync(Guid userId, string newPassword)
    {
        if (!await IsAdminAsync()) return Forbid();

        await _admin.SetPasswordAsync(userId, newPassword, "admin panel");
        StatusMessage = "Password updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignRoleAsync(Guid userId, string roleName)
    {
        if (!await IsAdminAsync()) return Forbid();

        await _admin.AssignRoleAsync(userId, roleName);
        StatusMessage = $"Role '{roleName}' assigned.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(Guid userId, string roleName)
    {
        if (!await IsAdminAsync()) return Forbid();

        await _admin.RemoveRoleAsync(userId, roleName);
        StatusMessage = $"Role '{roleName}' removed.";
        return RedirectToPage();
    }
}
