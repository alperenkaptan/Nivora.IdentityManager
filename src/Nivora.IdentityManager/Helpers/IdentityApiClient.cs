using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nivora.IdentityManager.Data;

namespace Nivora.IdentityManager.Helpers;

public class IdentityApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public IdentityApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _http = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private Uri BaseAddress
    {
        get
        {
            var request = _httpContextAccessor.HttpContext?.Request
                ?? throw new InvalidOperationException("No active HTTP request.");
            return new Uri($"{request.Scheme}://{request.Host}");
        }
    }

    public async Task<ApiResult<AuthTokens>> RegisterAsync(string email, string password)
    {
        var body = JsonContent(new { email, password });
        var response = await _http.PostAsync(new Uri(BaseAddress, "/auth/register"), body);
        return await ParseAsync<AuthTokens>(response);
    }

    public async Task<ApiResult<AuthTokens>> LoginAsync(string email, string password)
    {
        var body = JsonContent(new { email, password });
        var response = await _http.PostAsync(new Uri(BaseAddress, "/auth/login"), body);
        return await ParseAsync<AuthTokens>(response);
    }

    public async Task<ApiResult<AuthTokens>> RefreshAsync(string refreshToken)
    {
        var body = JsonContent(new { refreshToken });
        var response = await _http.PostAsync(new Uri(BaseAddress, "/auth/refresh"), body);
        return await ParseAsync<AuthTokens>(response);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var body = JsonContent(new { refreshToken });
        await _http.PostAsync(new Uri(BaseAddress, "/auth/logout"), body);
    }

    public async Task<ApiResult<MeInfo>> GetMeAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BaseAddress, "/auth/me"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _http.SendAsync(request);
        return await ParseAsync<MeInfo>(response);
    }

    public async Task<ApiResult<MeInfo>> GetMeWithRefreshAsync(ISession session)
    {
        var accessToken = AuthSessionStore.GetAccessToken(session);
        if (string.IsNullOrEmpty(accessToken))
            return ApiResult<MeInfo>.Fail("Not authenticated.");

        var result = await GetMeAsync(accessToken);
        if (result.Success)
            return result;

        var refreshToken = AuthSessionStore.GetRefreshToken(session);
        if (string.IsNullOrEmpty(refreshToken))
            return ApiResult<MeInfo>.Fail("Not authenticated.");

        var refreshResult = await RefreshAsync(refreshToken);
        if (!refreshResult.Success)
            return ApiResult<MeInfo>.Fail(refreshResult.Error ?? "Refresh failed.");

        AuthSessionStore.SetTokens(session, refreshResult.Data!.AccessToken, refreshResult.Data.RefreshToken);
        return await GetMeAsync(refreshResult.Data.AccessToken);
    }

    private static StringContent JsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj, JsonOpts), Encoding.UTF8, "application/json");

    private static async Task<ApiResult<T>> ParseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<T>(json, JsonOpts);
            return ApiResult<T>.Ok(data!);
        }

        // Try extract ProblemDetails detail
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return ApiResult<T>.Fail(detail.GetString() ?? response.ReasonPhrase ?? "Error");
        }
        catch { }

        return ApiResult<T>.Fail(response.ReasonPhrase ?? "Error");
    }
}

public static class AuthErrorHelper
{
    /// <summary>
    /// After a failed login/register, query the DB to return a more specific error
    /// (disabled, locked out) instead of the generic library message.
    /// </summary>
    public static async Task<string> EnrichErrorAsync(AppDbContext db, string email, string fallbackError)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await db.Set<IdentityUserRow>()
            .FromSqlRaw(
                "SELECT Id, Email, NormalizedEmail, IsDisabled, AccessFailedCount, LockoutEnd, CreatedAt, LastLoginAt " +
                "FROM nivora_identity_users WHERE NormalizedEmail = {0}", normalizedEmail)
            .FirstOrDefaultAsync();

        if (user is null)
            return fallbackError;

        if (user.IsDisabled)
            return "This account has been disabled. Contact an administrator.";

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            return $"This account is locked out until {user.LockoutEnd.Value.LocalDateTime:g}. Please try again later.";

        return fallbackError;
    }
}

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResult<T> Fail(string error) => new() { Success = false, Error = error };
}

public class AuthTokens
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresIn { get; set; }
}

public class MeInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
