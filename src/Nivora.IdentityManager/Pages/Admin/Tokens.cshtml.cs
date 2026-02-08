using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.IdentityManager.Helpers;

namespace Nivora.IdentityManager.Pages.Admin;

public class TokensModel : PageModel
{
    private readonly IIdentityAdminService _admin;

    public TokensModel(IIdentityAdminService admin)
    {
        _admin = admin;
    }

    public bool IsForbidden { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? GeneratedToken { get; set; }

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
        }
        return Page();
    }

    public async Task<IActionResult> OnPostPasswordResetAsync(string email)
    {
        if (!await IsAdminAsync()) return Forbid();

        var token = await _admin.CreatePasswordResetTokenAsync(email);
        if (token is not null)
        {
            GeneratedToken = token;
            StatusMessage = $"Password reset token generated for {email}.";
        }
        else
        {
            StatusMessage = $"Could not generate token for {email} (user not found, disabled, or other issue).";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEmailConfirmationAsync(string email)
    {
        if (!await IsAdminAsync()) return Forbid();

        var token = await _admin.CreateEmailConfirmationTokenAsync(email);
        if (token is not null)
        {
            GeneratedToken = token;
            StatusMessage = $"Email confirmation token generated for {email}.";
        }
        else
        {
            StatusMessage = $"Could not generate token for {email} (not found, disabled, or already confirmed).";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPhoneVerificationAsync(Guid userId)
    {
        if (!await IsAdminAsync()) return Forbid();

        var code = await _admin.CreatePhoneVerificationCodeAsync(userId);
        if (code is not null)
        {
            GeneratedToken = code;
            StatusMessage = $"Phone verification code generated for user {userId}.";
        }
        else
        {
            StatusMessage = $"Could not generate code for user {userId} (not found, disabled, or no phone set).";
        }
        return RedirectToPage();
    }
}
