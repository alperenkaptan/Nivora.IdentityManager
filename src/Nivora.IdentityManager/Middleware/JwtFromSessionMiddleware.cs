namespace Nivora.IdentityManager.Middleware;

/// <summary>
/// Middleware that extracts JWT token from session and adds it to Authorization header.
/// This allows [Authorize] attributes to work with session-based authentication in Razor Pages.
/// </summary>
public class JwtFromSessionMiddleware
{
    private readonly RequestDelegate _next;

    public JwtFromSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get JWT from session
        var token = context.Session.GetString("access_token");
        
        // If token exists and Authorization header is not already set, add it
        if (!string.IsNullOrEmpty(token) && string.IsNullOrEmpty(context.Request.Headers.Authorization))
        {
            context.Request.Headers.Authorization = $"Bearer {token}";
        }

        await _next(context);
    }
}
