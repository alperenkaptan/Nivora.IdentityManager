namespace Nivora.IdentityManager.Helpers;

public static class AuthSessionStore
{
    private const string RefreshTokenKey = "refresh_token";
    private const string ChallengeTokenKey = "2fa_challenge";

    public static void SetRefreshToken(ISession session, string refreshToken) =>
        session.SetString(RefreshTokenKey, refreshToken);

    public static string? GetRefreshToken(ISession session) =>
        session.GetString(RefreshTokenKey);

    public static void SetChallengeToken(ISession session, string token) =>
        session.SetString(ChallengeTokenKey, token);

    public static string? GetChallengeToken(ISession session) =>
        session.GetString(ChallengeTokenKey);

    public static void ClearChallenge(ISession session) =>
        session.Remove(ChallengeTokenKey);

    public static void Clear(ISession session)
    {
        session.Remove(RefreshTokenKey);
        session.Remove(ChallengeTokenKey);
    }
}
