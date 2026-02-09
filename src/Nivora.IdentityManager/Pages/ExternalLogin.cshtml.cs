using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Contracts.Dtos;
using Nivora.IdentityManager.Auth;
using Nivora.IdentityManager.Data;
using Nivora.IdentityManager.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Nivora.IdentityManager.Pages;

public class ExternalLoginModel : PageModel
{
    private readonly INivoraIdentityFacade _facade;
    private readonly AppDbContext _db;
    private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptions;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        INivoraIdentityFacade facade,
        AppDbContext db,
        IOptionsMonitor<JwtBearerOptions> jwtOptions,
        ILogger<ExternalLoginModel> logger)
    {
        _facade = facade;
        _db = db;
        _jwtOptions = jwtOptions;
        _logger = logger;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string provider, string providerUserId, string? email)
    {
        try
        {
            var ctx = IdentityCallContext.FromHttp(HttpContext);
            var response = await _facade.ExternalLoginAsync(
                new ExternalLoginRequest(provider, providerUserId, email), ctx);

            // Always trust the token (signed by Nivora.Identity), not the parameter
            var (userId, tokenEmail) = ValidateTokenAndExtractUser(response.AccessToken);

            // Email resolution: token claim is authoritative
            // Fallback to DB only if token doesn't include email
            var resolvedEmail = tokenEmail ?? await GetEmailFromDatabaseAsync(userId);

            if (string.IsNullOrWhiteSpace(resolvedEmail))
            {
                ErrorMessage = "Unable to resolve email from token or database. External login setup may be incomplete.";
                return RedirectToPage();
            }

            // Log if caller provided email different from token (potential config mismatch)
            if (!string.IsNullOrWhiteSpace(email) && !string.Equals(email, tokenEmail, StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogWarning("External login email mismatch detected. Using token email value. User ID: {UserId}", userId);
            }

            await CookieSignInHelper.SignInAsync(HttpContext, userId, resolvedEmail);

            // SECURITY FIX: Check if user is disabled
            var user = await _db.Set<IdentityUserRow>()
                .FromSqlRaw(
                    "SELECT Id, Email, NormalizedEmail, IsDisabled, AccessFailedCount, LockoutEnd, CreatedAt, LastLoginAt, EmailConfirmedAt, PhoneNumber, PhoneConfirmedAt, TwoFactorEnabled FROM Users WHERE Id = {0}",
                    userId)
                .FirstOrDefaultAsync();

            if (user?.IsDisabled == true)
            {
                ErrorMessage = "This account has been disabled.";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Login");
            }

            AuthSessionStore.SetRefreshToken(HttpContext.Session, response.RefreshToken);
            return RedirectToPage("/Profile");
        }
        catch (SecurityTokenException)
        {
            ErrorMessage = "Invalid or expired authentication token. Please try again.";
            return RedirectToPage();
        }
        catch (IdentityOperationException ex)
        {
            ErrorMessage = ex.Detail;
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during external login");
            ErrorMessage = "An unexpected error occurred during external login. Please try again.";
            return RedirectToPage();
        }
    }

    /// <summary>
    /// Validates the access token using registered JWT Bearer parameters
    /// and extracts userId (from "sub" claim) and email (if present).
    /// Token validation ensures signature, expiry, issuer, and audience are correct.
    /// </summary>
    private (Guid UserId, string? Email) ValidateTokenAndExtractUser(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required.", nameof(accessToken));

        var options = _jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);
        if (options?.TokenValidationParameters is null)
            throw new InvalidOperationException("JWT Bearer authentication is not configured.");

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(accessToken, options.TokenValidationParameters, out _);

        // Extract user ID (sub is standard, NameIdentifier is fallback)
        var sub = principal.FindFirstValue("sub")
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (sub is null || !Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException("Token does not contain a valid 'sub' claim.");

        // Extract email from token (multiple claim name variants for compatibility)
        var email = principal.FindFirstValue("email")
                ?? principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email);

        return (userId, email);
    }

    /// <summary>
    /// Fallback: resolve email from database when not present in token.
    /// Note: This query uses "Users" table name (Nivora.Identity default).
    /// If you customize the table name in OnModelCreating, update this query accordingly.
    /// </summary>
    private async Task<string?> GetEmailFromDatabaseAsync(Guid userId)
    {
        try
        {
            var userRow = await _db.Set<IdentityUserRow>()
                .FromSqlRaw(
                    "SELECT Id, Email, NormalizedEmail, IsDisabled, AccessFailedCount, LockoutEnd, CreatedAt, LastLoginAt, EmailConfirmedAt, PhoneNumber, PhoneConfirmedAt, TwoFactorEnabled FROM Users WHERE Id = {0}",
                    userId)
                .FirstOrDefaultAsync();

            return userRow?.Email;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve email from database for userId {UserId}", userId);
            return null;
        }
    }
}
