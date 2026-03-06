using PiSearch.Core.Algorithms;

namespace PiSearch.Tests;

public sealed class BoyerMooreSearcherTests
{
    // ─── Construction ──────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsOnNullPattern()
    {
        Assert.Throws<ArgumentException>(() => new BoyerMooreSearcher(null!));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyPattern()
    {
        Assert.Throws<ArgumentException>(() => new BoyerMooreSearcher(""));
    }

    [Fact]
    public void Pattern_Property_ReturnsInput()
    {
        var bm = new BoyerMooreSearcher("1234");
        Assert.Equal("1234", bm.Pattern);
    }

    // ─── Search – found ────────────────────────────────────────────────────

    [Theory]
    [InlineData("hello world", "hello", 0)]
    [InlineData("hello world", "world", 6)]
    [InlineData("abcabc", "abc", 0)]
    [InlineData("31415926535", "926535", 5)]
    public void Search_FindsPatternAtCorrectOffset(string text, string pattern, int expectedIndex)
    {
        var bm = new BoyerMooreSearcher(pattern);
        byte[] textBytes = System.Text.Encoding.ASCII.GetBytes(text);
        Assert.Equal(expectedIndex, bm.Search(textBytes));
    }

    [Fact]
    public void Search_FindsPatternAtStart()
    {
        var bm = new BoyerMooreSearcher("314");
        byte[] text = System.Text.Encoding.ASCII.GetBytes("3141592653");
        Assert.Equal(0, bm.Search(text));
    }

    [Fact]
    public void Search_FindsPatternAtEnd()
    {
        var bm = new BoyerMooreSearcher("653");
        byte[] text = System.Text.Encoding.ASCII.GetBytes("3141592653");
        Assert.Equal(7, bm.Search(text));
    }

    [Fact]
    public void Search_PatternEqualsText_ReturnsZero()
    {
        var bm = new BoyerMooreSearcher("314");
        byte[] text = System.Text.Encoding.ASCII.GetBytes("314");
        Assert.Equal(0, bm.Search(text));
    }

    // ─── Search – not found ────────────────────────────────────────────────

    [Fact]
    public void Search_ReturnsMinusOne_WhenNotFound()
    {
        var bm = new BoyerMooreSearcher("99999");
        byte[] text = System.Text.Encoding.ASCII.GetBytes("31415926535");
        Assert.Equal(-1, bm.Search(text));
    }

    [Fact]
    public void Search_PatternLongerThanText_ReturnsMinusOne()
    {
        var bm = new BoyerMooreSearcher("12345678901234567890");
        byte[] text = System.Text.Encoding.ASCII.GetBytes("123");
        Assert.Equal(-1, bm.Search(text));
    }

    // ─── SearchAll ─────────────────────────────────────────────────────────

    [Fact]
    public void SearchAll_FindsMultipleOccurrences()
    {
        var bm = new BoyerMooreSearcher("11");
        byte[] text = System.Text.Encoding.ASCII.GetBytes("1141114111");
        var hits = bm.SearchAll(text);
        // "11" appears at indices 0, 4, 7
        Assert.Contains(0, hits);
        Assert.Contains(4, hits);
        Assert.Contains(7, hits);
    }

    [Fact]
    public void SearchAll_EmptyResult_WhenPatternAbsent()
    {
        var bm = new BoyerMooreSearcher("99");
        byte[] text = System.Text.Encoding.ASCII.GetBytes("31415926");
        Assert.Empty(bm.SearchAll(text));
    }

    // ─── Boyer-Moore specific: pattern with repeated characters ───────────

    [Fact]
    public void Search_RepeatedCharPattern_FindsCorrectly()
    {
        var bm = new BoyerMooreSearcher("aaa");
        byte[] text = System.Text.Encoding.ASCII.GetBytes("bbbaaabbb");
        Assert.Equal(3, bm.Search(text));
    }
}
