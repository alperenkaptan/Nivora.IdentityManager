using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Nivora.IdentityManager.Auth;

public static class CookieSignInHelper
{
    public static async Task SignInFromJwtAsync(HttpContext httpContext, string accessToken)
    {
        var (userId, email) = ParseJwtPayload(accessToken);
        if (userId is null)
            throw new InvalidOperationException("Cannot extract user id from token.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
        };

        if (email is not null)
        {
            claims.Add(new(ClaimTypes.Email, email));
            claims.Add(new(ClaimTypes.Name, email));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    private static (Guid? UserId, string? Email) ParseJwtPayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return (null, null);

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);

            Guid? userId = null;
            string? email = null;

            if (doc.RootElement.TryGetProperty("sub", out var sub))
                userId = Guid.Parse(sub.GetString()!);

            if (doc.RootElement.TryGetProperty("email", out var emailProp))
                email = emailProp.GetString();

            return (userId, email);
        }
        catch
        {
            return (null, null);
        }
    }
}
