using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;
using Nivora.IdentityManager.Helpers;
using System.Security.Claims;

namespace Nivora.IdentityManager.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;

    public ProfileModel(INivoraIdentityFacade facade) => _facade = facade;

    public MeResponse? Me { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? TotpSecret { get; set; }

    [TempData]
    public string? TotpUri { get; set; }

    private Guid? CurrentUserId
    {
        get
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return sub is not null && Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            ErrorMessage = "Not authenticated.";
            return Page();
        }

        try
        {
            Me = await _facade.MeAsync(userId.Value, IdentityCallContext.FromHttp(HttpContext));
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return Page();
    }

    // ?? Logout ??
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        var refreshToken = AuthSessionStore.GetRefreshToken(HttpContext.Session);
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try { await _facade.LogoutAsync(new LogoutRequest(refreshToken), IdentityCallContext.FromHttp(HttpContext)); }
            catch { /* best-effort */ }
        }
        AuthSessionStore.Clear(HttpContext.Session);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }

    // ?? Change Password ??
    public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword)
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.ChangePasswordAsync(userId.Value,
                new ChangePasswordRequest(currentPassword, newPassword),
                IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = "Password changed successfully. Please log in again.";
            AuthSessionStore.Clear(HttpContext.Session);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
            return RedirectToPage();
        }
    }

    // ?? Email Confirmation Request ??
    public async Task<IActionResult> OnPostRequestEmailConfirmationAsync()
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.RequestEmailConfirmationAsync(userId.Value, IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = "Email confirmation requested. An admin can generate the token from the Admin panel.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }

    // ?? Phone: Set ??
    public async Task<IActionResult> OnPostSetPhoneAsync(string phoneNumber)
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.SetPhoneAsync(userId.Value, new SetPhoneRequest(phoneNumber), IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = "Phone number updated.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }

    // ?? Phone: Request Verification ??
    public async Task<IActionResult> OnPostRequestPhoneVerificationAsync(string phoneNumber)
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.RequestPhoneVerificationAsync(userId.Value, new SetPhoneRequest(phoneNumber), IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = "Phone verification requested. An admin can generate the code from the Admin panel.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }

    // ?? Phone: Confirm ??
    public async Task<IActionResult> OnPostConfirmPhoneAsync(string code)
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.ConfirmPhoneAsync(userId.Value, new ConfirmPhoneRequest(code), IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = "Phone number confirmed.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }

    // ?? 2FA TOTP: Setup ??
    public async Task<IActionResult> OnPostTotpSetupAsync()
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            var setup = await _facade.SetupTotpAsync(userId.Value, IdentityCallContext.FromHttp(HttpContext));
            TotpSecret = setup.SecretBase32;
            TotpUri = setup.OtpAuthUri;
            StatusMessage = "TOTP secret generated. Use your authenticator app to scan it.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }

    // ?? 2FA TOTP: Enable ??
    public async Task<IActionResult> OnPostTotpEnableAsync(string code)
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.EnableTotpAsync(userId.Value, new TotpEnableRequest(code), IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = "Two-factor authentication enabled.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }

    // ?? 2FA TOTP: Disable ??
    public async Task<IActionResult> OnPostTotpDisableAsync(string code)
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.DisableTotpAsync(userId.Value, new TotpDisableRequest(code), IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = "Two-factor authentication disabled.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }

    // ?? External Login: Link ??
    public async Task<IActionResult> OnPostLinkExternalAsync(string provider, string providerUserId)
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.LinkExternalLoginAsync(userId.Value,
                new ExternalLoginLinkRequest(provider, providerUserId),
                IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = $"External login '{provider}' linked.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }

    // ?? External Login: Unlink ??
    public async Task<IActionResult> OnPostUnlinkExternalAsync(string provider)
    {
        var userId = CurrentUserId;
        if (userId is null) return RedirectToPage("/Login");

        try
        {
            await _facade.UnlinkExternalLoginAsync(userId.Value,
                new ExternalLoginUnlinkRequest(provider),
                IdentityCallContext.FromHttp(HttpContext));
            StatusMessage = $"External login '{provider}' unlinked.";
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
        }
        return RedirectToPage();
    }
}
