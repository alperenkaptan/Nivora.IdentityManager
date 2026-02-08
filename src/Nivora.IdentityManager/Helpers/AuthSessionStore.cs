namespace Nivora.IdentityManager.Helpers;

public static class AuthSessionStore
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    public static void SetTokens(ISession session, string accessToken, string refreshToken)
    {
        session.SetString(AccessTokenKey, accessToken);
        session.SetString(RefreshTokenKey, refreshToken);
    }

    public static string? GetAccessToken(ISession session) =>
        session.GetString(AccessTokenKey);

    public static string? GetRefreshToken(ISession session) =>
        session.GetString(RefreshTokenKey);

    public static void Clear(ISession session)
    {
        session.Remove(AccessTokenKey);
        session.Remove(RefreshTokenKey);
    }
}
