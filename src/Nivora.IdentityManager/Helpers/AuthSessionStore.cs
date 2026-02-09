namespace Nivora.IdentityManager.Helpers;

public static class AuthSessionStore
{
    private const string RefreshTokenKey = "refresh_token";
    private const string ChallengeTokenKey = "2fa_challenge";
    private const string LoginEmailKey = "login_email";

    public static void SetRefreshToken(ISession session, string refreshToken) =>
        session.SetString(RefreshTokenKey, refreshToken);

    public static string? GetRefreshToken(ISession session) =>
        session.GetString(RefreshTokenKey);

    public static void SetChallengeToken(ISession session, string token) =>
        session.SetString(ChallengeTokenKey, token);

    public static string? GetChallengeToken(ISession session) =>
        session.GetString(ChallengeTokenKey);

    public static void SetLoginEmail(ISession session, string email) =>
        session.SetString(LoginEmailKey, email);

    public static string? GetLoginEmail(ISession session) =>
        session.GetString(LoginEmailKey);

    public static void ClearChallenge(ISession session)
    {
        session.Remove(ChallengeTokenKey);
        session.Remove(LoginEmailKey);
    }

    public static void Clear(ISession session)
    {
        session.Remove(RefreshTokenKey);
        session.Remove(ChallengeTokenKey);
        session.Remove(LoginEmailKey);
    }
}
