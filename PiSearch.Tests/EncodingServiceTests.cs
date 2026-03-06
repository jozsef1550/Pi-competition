using PiSearch.Core.Models;
using PiSearch.Core.Services;

namespace PiSearch.Tests;

public sealed class EncodingServiceTests
{
    private readonly EncodingService _svc = new();

    // ─── ASCII encoding ────────────────────────────────────────────────────

    [Fact]
    public void Encode_Ascii_ConvertsLetterToDecimalCodePoint()
    {
        // 'A' = 65
        Assert.Equal("65", _svc.Encode("A", EncodingMethod.Ascii));
    }

    [Fact]
    public void Encode_Ascii_MultipleChars_Concatenates()
    {
        // 'A'=65, '!'=33
        Assert.Equal("6533", _svc.Encode("A!", EncodingMethod.Ascii));
    }

    [Fact]
    public void Encode_Ascii_Formula_CorrectOutput()
    {
        // "1+1=2" → 49 43 49 61 50 → "4943496150"
        Assert.Equal("4943496150", _svc.Encode("1+1=2", EncodingMethod.Ascii));
    }

    [Fact]
    public void Encode_Ascii_WithPositiveOffset_AddsOffset()
    {
        // 'A'=65, offset=1 → 66
        Assert.Equal("66", _svc.Encode("A", EncodingMethod.Ascii, offset: 1));
    }

    [Fact]
    public void Encode_Ascii_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _svc.Encode("", EncodingMethod.Ascii));
    }

    // ─── Custom Offset encoding ────────────────────────────────────────────

    [Fact]
    public void Encode_CustomOffset_A_MapsTo01()
    {
        Assert.Equal("01", _svc.Encode("A", EncodingMethod.CustomOffset));
    }

    [Fact]
    public void Encode_CustomOffset_Z_MapsTo26()
    {
        Assert.Equal("26", _svc.Encode("Z", EncodingMethod.CustomOffset));
    }

    [Fact]
    public void Encode_CustomOffset_LowercaseA_MapsTo01()
    {
        Assert.Equal("01", _svc.Encode("a", EncodingMethod.CustomOffset));
    }

    [Fact]
    public void Encode_CustomOffset_Digit0_MapsTo00()
    {
        Assert.Equal("00", _svc.Encode("0", EncodingMethod.CustomOffset));
    }

    [Fact]
    public void Encode_CustomOffset_Digit9_MapsTo09()
    {
        Assert.Equal("09", _svc.Encode("9", EncodingMethod.CustomOffset));
    }

    [Fact]
    public void Encode_CustomOffset_WithOffset_ShiftsValues()
    {
        // 'A'=1, offset=1 → 2 → "02"
        Assert.Equal("02", _svc.Encode("A", EncodingMethod.CustomOffset, offset: 1));
    }

    [Fact]
    public void Encode_CustomOffset_Word_ConcatenatesCorrectly()
    {
        // "AB" → "01" + "02" = "0102"
        Assert.Equal("0102", _svc.Encode("AB", EncodingMethod.CustomOffset));
    }

    // ─── Base-16 encoding ─────────────────────────────────────────────────

    [Fact]
    public void Encode_Base16_A_Returns41()
    {
        // 'A' = 0x41
        Assert.Equal("41", _svc.Encode("A", EncodingMethod.Base16));
    }

    [Fact]
    public void Encode_Base16_ExclamationMark_Returns21()
    {
        // '!' = 0x21
        Assert.Equal("21", _svc.Encode("!", EncodingMethod.Base16));
    }

    [Fact]
    public void Encode_Base16_IsUppercase()
    {
        // 'a' = 0x61
        Assert.Equal("61", _svc.Encode("a", EncodingMethod.Base16));
    }

    [Fact]
    public void Encode_Base16_WithOffset()
    {
        // 'A'=65=0x41, offset=1 → 66=0x42
        Assert.Equal("42", _svc.Encode("A", EncodingMethod.Base16, offset: 1));
    }

    // ─── GetCharacterMap ───────────────────────────────────────────────────

    [Fact]
    public void GetCharacterMap_ReturnsOneEntryPerChar()
    {
        var map = _svc.GetCharacterMap("AB", EncodingMethod.Ascii).ToList();
        Assert.Equal(2, map.Count);
        Assert.Equal('A', map[0].Character);
        Assert.Equal('B', map[1].Character);
    }

    [Fact]
    public void GetCharacterMap_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(_svc.GetCharacterMap("", EncodingMethod.Ascii));
    }

    // ─── Invalid encoding ─────────────────────────────────────────────────

    [Fact]
    public void Encode_InvalidMethod_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _svc.Encode("A", (EncodingMethod)99));
    }
}
