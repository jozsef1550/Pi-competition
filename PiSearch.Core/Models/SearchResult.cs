namespace PiSearch.Core.Models;

/// <summary>
/// Represents a single match found within the digits of π.
/// </summary>
public sealed class SearchResult
{
    /// <summary>The zero-based index of the first digit in π where the match begins.</summary>
    public long Index { get; set; }

    /// <summary>The encoded digit string that was actually matched.</summary>
    public string MatchedPattern { get; set; } = string.Empty;

    /// <summary>The original search text that produced <see cref="MatchedPattern"/>.</summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>Encoding method used for this result.</summary>
    public EncodingMethod Encoding { get; set; }

    /// <summary>Offset applied to character codes for this result.</summary>
    public int Offset { get; set; }

    /// <summary>Total number of digits scanned before this match was found.</summary>
    public long DigitsScanned { get; set; }

    /// <summary>Elapsed time from start of search to discovery of this match.</summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>Human-readable label from the originating <see cref="SearchOptions"/>.</summary>
    public string Label { get; set; } = string.Empty;
}
