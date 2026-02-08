using System.Text;
using System.Text.Json;

namespace Nivora.IdentityManager.Helpers;

public static class AuthSessionStore
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private const string UserIdKey = "user_id";
    private const string ChallengeTokenKey = "2fa_challenge";

    public static void SetTokens(ISession session, string accessToken, string refreshToken)
    {
        session.SetString(AccessTokenKey, accessToken);
        session.SetString(RefreshTokenKey, refreshToken);

        var userId = ExtractUserIdFromJwt(accessToken);
        if (userId.HasValue)
            session.SetString(UserIdKey, userId.Value.ToString());
    }

    public static string? GetAccessToken(ISession session) =>
        session.GetString(AccessTokenKey);

    public static string? GetRefreshToken(ISession session) =>
        session.GetString(RefreshTokenKey);

    public static Guid? GetUserId(ISession session)
    {
        var val = session.GetString(UserIdKey);
        return val is not null && Guid.TryParse(val, out var id) ? id : null;
    }

    public static void SetChallengeToken(ISession session, string token) =>
        session.SetString(ChallengeTokenKey, token);

    public static string? GetChallengeToken(ISession session) =>
        session.GetString(ChallengeTokenKey);

    public static void ClearChallenge(ISession session) =>
        session.Remove(ChallengeTokenKey);

    public static void Clear(ISession session)
    {
        session.Remove(AccessTokenKey);
        session.Remove(RefreshTokenKey);
        session.Remove(UserIdKey);
        session.Remove(ChallengeTokenKey);
    }

    private static Guid? ExtractUserIdFromJwt(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("sub", out var sub))
                return Guid.Parse(sub.GetString()!);
            return null;
        }
        catch
        {
            return null;
        }
    }
}
