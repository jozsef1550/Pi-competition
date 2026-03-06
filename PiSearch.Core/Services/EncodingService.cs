using PiSearch.Core.Models;

namespace PiSearch.Core.Services;

/// <summary>
/// Converts a raw search string into the digit sequence to look for inside π,
/// according to the chosen <see cref="EncodingMethod"/>.
/// </summary>
public sealed class EncodingService
{
    // ─── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Encodes <paramref name="text"/> according to the requested method and offset,
    /// returning the concatenated digit/hex string to search for.
    /// </summary>
    /// <param name="text">Raw input text (may contain letters, digits, symbols, formulas).</param>
    /// <param name="method">Encoding strategy to apply.</param>
    /// <param name="offset">
    /// Integer added to every character code point before conversion.
    /// Use positive values to try "shifted" keys.
    /// </param>
    public string Encode(string text, EncodingMethod method, int offset = 0)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return method switch
        {
            EncodingMethod.Ascii        => EncodeAscii(text, offset),
            EncodingMethod.CustomOffset => EncodeCustomOffset(text, offset),
            EncodingMethod.Base16       => EncodeBase16(text, offset),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
    }

    /// <summary>
    /// Returns a human-readable description of the character-to-number mapping
    /// for all characters in <paramref name="text"/>.
    /// </summary>
    public IEnumerable<(char Character, string Encoded)> GetCharacterMap(
        string text, EncodingMethod method, int offset = 0)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        foreach (char c in text)
        {
            int code = c + offset;
            string encoded = method switch
            {
                EncodingMethod.Ascii        => code.ToString(),
                EncodingMethod.CustomOffset => EncodeCustomChar(c, offset),
                EncodingMethod.Base16       => Convert.ToString(code, 16).ToUpperInvariant(),
                _ => code.ToString()
            };
            yield return (c, encoded);
        }
    }

    // ─── Encoding implementations ─────────────────────────────────────────

    /// <summary>
    /// Standard ASCII encoding: each char → its decimal code-point string.
    /// 'A' (65) + offset=0 → "65"; 'A' + offset=1 → "66".
    /// Digits are left as their own code points: '0'=48, '1'=49, …
    /// </summary>
    private static string EncodeAscii(string text, int offset)
    {
        var sb = new System.Text.StringBuilder(text.Length * 3);
        foreach (char c in text)
            sb.Append(c + offset);
        return sb.ToString();
    }

    /// <summary>
    /// Custom alphabet offset: A/a → 01, B/b → 02, … Z/z → 26.
    /// Digits '0'–'9' map to their numeric value (0–9, printed as 1 char).
    /// Other characters fall back to their ASCII code + offset.
    /// </summary>
    private static string EncodeCustomOffset(string text, int offset)
    {
        var sb = new System.Text.StringBuilder(text.Length * 3);
        foreach (char c in text)
            sb.Append(EncodeCustomChar(c, offset));
        return sb.ToString();
    }

    private static string EncodeCustomChar(char c, int offset)
    {
        int code;
        if (c >= 'A' && c <= 'Z')
            code = (c - 'A' + 1) + offset;        // A=1 … Z=26
        else if (c >= 'a' && c <= 'z')
            code = (c - 'a' + 1) + offset;        // a=1 … z=26
        else if (c >= '0' && c <= '9')
            code = (c - '0') + offset;             // '0'=0 … '9'=9
        else
            code = (int)c + offset;                // fallback: raw code point

        return code.ToString("D2");                // zero-pad to at least 2 digits
    }

    /// <summary>
    /// Base-16 encoding: each char → uppercase hex code-point string.
    /// 'A' (0x41) → "41"; '!' (0x21) → "21".
    /// </summary>
    private static string EncodeBase16(string text, int offset)
    {
        var sb = new System.Text.StringBuilder(text.Length * 3);
        foreach (char c in text)
            sb.Append(Convert.ToString((int)c + offset, 16).ToUpperInvariant());
        return sb.ToString();
    }
}
