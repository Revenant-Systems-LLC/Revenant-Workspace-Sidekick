namespace RevenantWorkspaceSidekick.Core;

public static class EntropyScorer
{
    /// <summary>
    /// Shannon entropy in bits per character.  A truly random Base64 string is ~6.0.
    /// High-entropy threshold for secret detection: ≥ 4.5 bpc (empirically low false-positive rate).
    /// </summary>
    public static double Shannon(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return 0;

        Span<int> freq = stackalloc int[128];
        var nonAscii = 0;

        foreach (var c in value)
        {
            if (c < 128) freq[c]++;
            else nonAscii++;
        }

        var len = (double)value.Length;
        var entropy = 0.0;
        foreach (var count in freq)
        {
            if (count == 0) continue;
            var p = count / len;
            entropy -= p * Math.Log2(p);
        }
        if (nonAscii > 0)
        {
            var p = nonAscii / len;
            entropy -= p * Math.Log2(p);
        }
        return entropy;
    }

    public static bool IsHighEntropy(string value, double threshold = 4.5) =>
        value.Length >= 20 && Shannon(value) >= threshold;
}
