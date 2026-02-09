using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nivora.Identity.Abstractions;
using Nivora.IdentityManager.Data;

namespace Nivora.IdentityManager.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly IIdentityAdminService _admin;
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public UsersModel(IIdentityAdminService admin, AppDbContext db, IMemoryCache cache)
    {
        _admin = admin;
        _db = db;
        _cache = cache;
    }

    public List<IdentityUserRow> Users { get; set; } = [];
    public Dictionary<Guid, List<string>> UserRoles { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
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
        await _admin.DisableUserAsync(userId, "admin panel");
        // Disable edilen user'?n sessionlar?n? revoke et
        await _admin.RevokeAllSessionsAsync(userId, "user disabled");
        StatusMessage = "User disabled and sessions revoked.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEnableAsync(Guid userId)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE Users SET IsDisabled = 0 WHERE Id = {0}", userId);
        StatusMessage = "User enabled.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid userId)
    {
        await _admin.RevokeAllSessionsAsync(userId, "admin panel");
        StatusMessage = "All sessions revoked.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetPasswordAsync(Guid userId, string newPassword)
    {
        await _admin.SetPasswordAsync(userId, newPassword, "admin panel");
        // Password de?i?ti?inde user'?n tüm sessionlar?n? revoke et
        await _admin.RevokeAllSessionsAsync(userId, "password changed by admin");
        StatusMessage = "Password updated and sessions revoked.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignRoleAsync(Guid userId, string roleName)
    {
        await _admin.AssignRoleAsync(userId, roleName);
        _cache.Remove($"user-roles-{userId}");
        await _admin.RevokeAllSessionsAsync(userId, $"role '{roleName}' assigned");
        StatusMessage = $"Role '{roleName}' assigned and user sessions revoked (must re-login for role to take effect).";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(Guid userId, string roleName)
    {
        await _admin.RemoveRoleAsync(userId, roleName);
        _cache.Remove($"user-roles-{userId}");
        await _admin.RevokeAllSessionsAsync(userId, $"role '{roleName}' removed");
        StatusMessage = $"Role '{roleName}' removed and user sessions revoked (must re-login).";
        return RedirectToPage();
    }
}
