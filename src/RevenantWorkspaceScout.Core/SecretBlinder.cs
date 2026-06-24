namespace RevenantWorkspaceScout.Core;

/// <summary>
/// Produces a blinded preview of a matched secret value for inclusion in reports.
/// Reports never contain plaintext secrets; this is the redaction gate.
/// </summary>
public static class SecretBlinder
{
    public static string Blind(string secret)
    {
        if (secret.Length == 0) return string.Empty;
        if (secret.Length <= 8) return new string('*', secret.Length);
        return string.Concat(secret.AsSpan(0, 4), "****", secret.AsSpan(secret.Length - 4));
    }
}
