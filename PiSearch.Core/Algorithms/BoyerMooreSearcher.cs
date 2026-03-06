namespace PiSearch.Core.Algorithms;

/// <summary>
/// Boyer-Moore string-search algorithm implementation.
/// Uses both the Bad-Character heuristic and the Good-Suffix heuristic
/// to skip large sections of the text and achieve sub-linear average-case performance.
/// </summary>
public sealed class BoyerMooreSearcher
{
    private readonly byte[] _pattern;
    private readonly int[] _badChar;         // bad-character shift table (256 entries)
    private readonly int[] _goodSuffix;      // good-suffix shift table

    /// <summary>The encoded digit pattern being searched for.</summary>
    public string Pattern { get; }

    /// <param name="pattern">ASCII digit string to search for (e.g., "4943496150").</param>
    public BoyerMooreSearcher(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern must not be null or empty.", nameof(pattern));

        Pattern = pattern;
        _pattern = System.Text.Encoding.ASCII.GetBytes(pattern);
        _badChar = BuildBadCharTable(_pattern);
        _goodSuffix = BuildGoodSuffixTable(_pattern);
    }

    // ─── Table construction ────────────────────────────────────────────────

    private static int[] BuildBadCharTable(byte[] pat)
    {
        const int AlphabetSize = 256;
        int m = pat.Length;
        int[] table = new int[AlphabetSize];
        Array.Fill(table, -1);
        for (int i = 0; i < m; i++)
            table[pat[i]] = i;
        return table;
    }

    private static int[] BuildGoodSuffixTable(byte[] pat)
    {
        int m = pat.Length;
        int[] shift = new int[m + 1];
        int[] border = new int[m + 1];

        // Phase 1: compute border array for reversed pattern
        int i = m;
        int j = m + 1;
        border[i] = j;
        while (i > 0)
        {
            while (j <= m && pat[i - 1] != pat[j - 1])
            {
                if (shift[j] == 0)
                    shift[j] = j - i;
                j = border[j];
            }
            i--;
            j--;
            border[i] = j;
        }

        // Phase 2: fill remaining entries
        j = border[0];
        for (i = 0; i <= m; i++)
        {
            if (shift[i] == 0)
                shift[i] = j;
            if (i == j)
                j = border[j];
        }

        return shift;
    }

    // ─── Search over ReadOnlySpan ──────────────────────────────────────────

    /// <summary>
    /// Searches <paramref name="text"/> for the first occurrence of the pattern
    /// and returns the zero-based index, or -1 if not found.
    /// </summary>
    public int Search(ReadOnlySpan<byte> text)
    {
        int n = text.Length;
        int m = _pattern.Length;

        if (m == 0) return 0;
        if (m > n) return -1;

        int s = 0; // shift of the pattern with respect to text
        while (s <= n - m)
        {
            int j = m - 1;

            while (j >= 0 && _pattern[j] == text[s + j])
                j--;

            if (j < 0)
                return s; // match found

            int bcShift = j - _badChar[text[s + j]];
            int gsShift = _goodSuffix[j + 1];
            s += Math.Max(bcShift, gsShift);
        }
        return -1;
    }

    /// <summary>
    /// Returns all zero-based offsets in <paramref name="text"/> where the pattern occurs.
    /// </summary>
    public List<int> SearchAll(ReadOnlySpan<byte> text)
    {
        var results = new List<int>();
        int n = text.Length;
        int m = _pattern.Length;

        if (m == 0 || m > n) return results;

        int s = 0;
        while (s <= n - m)
        {
            int j = m - 1;
            while (j >= 0 && _pattern[j] == text[s + j])
                j--;

            if (j < 0)
            {
                results.Add(s);
                s += _goodSuffix[0]; // advance past the match
            }
            else
            {
                int bcShift = j - _badChar[text[s + j]];
                int gsShift = _goodSuffix[j + 1];
                s += Math.Max(bcShift, gsShift);
            }
        }
        return results;
    }
}
