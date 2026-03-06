namespace PiSearch.Core.Models;

/// <summary>
/// Options controlling how a Pi-search is performed.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>The raw text or formula to search for (e.g., "Hello", "1+1=2").</summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>The encoding method to use when converting the text to a digit string.</summary>
    public EncodingMethod Encoding { get; set; } = EncodingMethod.Ascii;

    /// <summary>
    /// Integer offset added to every character code point before encoding.
    /// Used with <see cref="EncodingMethod.CustomOffset"/> or to try shifted keys.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>Maximum number of digits of π to scan (0 = no limit).</summary>
    public long MaxDigits { get; set; }

    /// <summary>Human-readable label for this search configuration (shown in UI).</summary>
    public string Label { get; set; } = string.Empty;
}
