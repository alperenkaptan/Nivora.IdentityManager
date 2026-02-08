using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nivora.Identity.Abstractions;
using Nivora.IdentityManager.Data;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages.Admin;

public class UsersModel : PageModel
{
    private readonly IdentityApiClient _api;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IIdentityAdminService _admin;

    public UsersModel(IdentityApiClient api, IConfiguration config, AppDbContext db, IIdentityAdminService admin)
    {
        _api = api;
        _config = config;
        _db = db;
        _admin = admin;
    }

    public List<IdentityUserRow> Users { get; set; } = [];
    public bool IsForbidden { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    private async Task<bool> IsAdminAsync()
    {
        var me = await _api.GetMeWithRefreshAsync(HttpContext.Session);
        var adminEmail = _config["AdminSeed:Email"];
        return me.Success && string.Equals(me.Data?.Email, adminEmail, StringComparison.OrdinalIgnoreCase);
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
                "SELECT Id, Email, NormalizedEmail, IsDisabled, AccessFailedCount, LockoutEnd, CreatedAt, LastLoginAt FROM nivora_identity_users ORDER BY CreatedAt DESC")
            .ToListAsync();

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
            "UPDATE nivora_identity_users SET IsDisabled = 0 WHERE Id = {0}", userId);
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
}
