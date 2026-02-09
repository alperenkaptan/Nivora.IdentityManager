namespace Nivora.IdentityManager.Helpers;

public static class AuthSessionStore
{
    private const string RefreshTokenKey = "refresh_token";
    private const string ChallengeTokenKey = "2fa_challenge";
    private const string LoginEmailKey = "login_email";
    private const string ChallengeTimestampKey = "2fa_challenge_ts";

    public static void SetChallengeToken(ISession session, string token)
    {
        session.SetString(ChallengeTokenKey, token);
        session.SetString(ChallengeTimestampKey, DateTimeOffset.UtcNow.ToString("O"));
    }

    public static bool IsChallengeExpired(ISession session, TimeSpan maxAge = default)
    {
        maxAge = maxAge == default ? TimeSpan.FromMinutes(5) : maxAge;
        var ts = session.GetString(ChallengeTimestampKey);
        if (ts is null)
            return true;
        if (!DateTimeOffset.TryParse(ts, out var created))
            return true;
        return DateTimeOffset.UtcNow - created > maxAge;
    }

    public static void SetRefreshToken(ISession session, string refreshToken) =>
        session.SetString(RefreshTokenKey, refreshToken);

    public static string? GetRefreshToken(ISession session) =>
        session.GetString(RefreshTokenKey);

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
