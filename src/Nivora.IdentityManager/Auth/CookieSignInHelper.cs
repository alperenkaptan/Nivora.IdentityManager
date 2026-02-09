using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Nivora.IdentityManager.Auth;

/// <summary>
/// Helper to create and sign-in a cookie-based principal.
/// Validates input to ensure cookie contains valid claims.
/// </summary>
public static class CookieSignInHelper
{
    public static async Task SignInAsync(HttpContext httpContext, Guid userId, string email)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
