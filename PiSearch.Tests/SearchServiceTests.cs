using PiSearch.Core.Models;
using PiSearch.Core.Services;

namespace PiSearch.Tests;

public sealed class SearchServiceTests : IDisposable
{
    // Use the embedded fallback (non-existent path triggers it)
    private readonly PiDigitReader _reader = new("__nonexistent_pi_file__.txt");
    private readonly SearchService _svc;

    public SearchServiceTests()
    {
        _svc = new SearchService(_reader);
    }

    public void Dispose() => _svc.Dispose();

    // ─── EncodingService integration ──────────────────────────────────────

    [Fact]
    public void SearchSingle_FindsAsciiEncodedDigit1_InEmbeddedPi()
    {
        // '1' in ASCII is 49. The embedded π starts "31415926…"
        // So we're looking for "49" in those digits.
        // The embedded pi contains "49" – let's verify it finds something.
        var opts = new SearchOptions
        {
            SearchText = "1",
            Encoding = EncodingMethod.Ascii,
            Label = "ASCII '1'",
        };
        var result = _svc.SearchSingle(opts);
        Assert.NotNull(result);
        Assert.Equal("49", result.MatchedPattern);
        Assert.True(result.Index >= 0);
    }

    [Fact]
    public void SearchSingle_ReturnsNull_ForUnsearchablePattern()
    {
        // A very long pattern unlikely to appear in ~1000 embedded digits
        var opts = new SearchOptions
        {
            SearchText = new string('X', 40), // 40 'X's → 40 × 3-digit ASCII codes
            Encoding = EncodingMethod.Ascii,
            Label = "impossible",
        };
        var result = _svc.SearchSingle(opts);
        Assert.Null(result);
    }

    [Fact]
    public void SearchSingle_Result_PopulatesAllFields()
    {
        var opts = new SearchOptions
        {
            SearchText = "3",    // ASCII '3' = 51
            Encoding = EncodingMethod.Ascii,
            Label = "TestLabel",
        };
        var result = _svc.SearchSingle(opts);
        Assert.NotNull(result);
        Assert.Equal("TestLabel", result.Label);
        Assert.Equal(EncodingMethod.Ascii, result.Encoding);
        Assert.Equal(0, result.Offset);
        Assert.Equal("3", result.OriginalText);
        Assert.Equal("51", result.MatchedPattern);
        Assert.True(result.DigitsScanned > 0);
        Assert.True(result.ElapsedTime >= TimeSpan.Zero);
    }

    // ─── Parallel search ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchParallelAsync_ReturnsFirstMatch()
    {
        var opts = new[]
        {
            new SearchOptions { SearchText = "9", Encoding = EncodingMethod.Ascii, Label = "ASCII-9" },
            new SearchOptions { SearchText = "A", Encoding = EncodingMethod.CustomOffset, Label = "Offset-A" },
        };
        var result = await _svc.SearchParallelAsync(opts);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchParallelAsync_EmptyOptions_ReturnsNull()
    {
        var result = await _svc.SearchParallelAsync(Array.Empty<SearchOptions>());
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAllKeysAsync_ReturnsSortedResults()
    {
        var opts = new[]
        {
            new SearchOptions { SearchText = "9", Encoding = EncodingMethod.Ascii, Label = "ASCII" },
            new SearchOptions { SearchText = "A", Encoding = EncodingMethod.CustomOffset, Label = "Offset" },
        };
        var results = await _svc.SearchAllKeysAsync(opts);
        // Results should be sorted by index
        for (int i = 1; i < results.Count; i++)
            Assert.True(results[i].Index >= results[i - 1].Index);
    }

    // ─── MaxDigits constraint ─────────────────────────────────────────────

    [Fact]
    public void SearchSingle_RespectsMaxDigitsConstraint()
    {
        // Searching only the first 5 digits of the embedded π: "31415"
        // The encoded pattern "5757" (ASCII '9','9' → 57,57) won't appear.
        var opts = new SearchOptions
        {
            SearchText = "99",
            Encoding = EncodingMethod.Ascii,
            MaxDigits = 5,
            Label = "MaxDigits",
        };
        // With only 5 digits available, a 4-digit pattern cannot match.
        var result = _svc.SearchSingle(opts);
        Assert.Null(result);
    }
}
