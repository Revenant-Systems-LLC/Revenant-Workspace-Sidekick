using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core;

/// <summary>
/// Optionally verifies whether a detected credential is actually live by making
/// a minimal authenticated HTTP request to the relevant provider.
/// Only runs when --verify is passed; never runs by default (no outbound traffic).
/// </summary>
public static class SecretVerifier
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    /// <summary>
    /// Returns true if the token appears to be a live/valid credential,
    /// false if it is rejected/revoked, and null if we cannot determine.
    /// </summary>
    public static async Task<bool?> VerifyAsync(Finding finding, string rawValue)
    {
        try
        {
            // GitHub PAT
            if (rawValue.StartsWith("ghp_") || rawValue.StartsWith("gho_") || rawValue.StartsWith("ghx_"))
                return await VerifyGitHub(rawValue);

            // OpenAI
            if (rawValue.StartsWith("sk-"))
                return await VerifyOpenAi(rawValue);

            // AWS access key — cheapest safe check: STS GetCallerIdentity
            if (rawValue.StartsWith("AKIA"))
                return null; // AWS requires HMAC signature; skip without secret key

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool?> VerifyGitHub(string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        req.Headers.TryAddWithoutValidation("Authorization", $"token {token}");
        req.Headers.TryAddWithoutValidation("User-Agent", "RWS-SecretVerifier/0.68");
        var resp = await Http.SendAsync(req);
        return resp.StatusCode == System.Net.HttpStatusCode.OK;
    }

    private static async Task<bool?> VerifyOpenAi(string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        var resp = await Http.SendAsync(req);
        // 200 = valid, 401 = invalid/revoked, 429 = valid but rate-limited
        return resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.TooManyRequests;
    }
}
