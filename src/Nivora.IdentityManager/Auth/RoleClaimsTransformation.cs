using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Nivora.Identity.Abstractions;

namespace Nivora.IdentityManager.Auth;

public class RoleClaimsTransformation : IClaimsTransformation
{
    private readonly IIdentityAdminService _admin;
    private readonly IMemoryCache _cache;

    public RoleClaimsTransformation(IIdentityAdminService admin, IMemoryCache cache)
    {
        _admin = admin;
        _cache = cache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return principal;

        var cacheKey = $"user-roles-{userId}";
        if (!_cache.TryGetValue(cacheKey, out List<string>? roles))
        {
            roles = (await _admin.GetUserRolesAsync(userId)).ToList();
            _cache.Set(cacheKey, roles, TimeSpan.FromMinutes(2));
        }

        var identity = new ClaimsIdentity();
        foreach (var role in roles!)
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        principal.AddIdentity(identity);
        return principal;
    }
}
