namespace PiSearch.Core.Models;

/// <summary>
/// Defines the encoding method used to convert a search string into a digit sequence.
/// </summary>
public enum EncodingMethod
{
    /// <summary>Standard ASCII/UTF-8 encoding (e.g., 'A' = 65, '!' = 33).</summary>
    Ascii,

    /// <summary>Custom alphabet offset (e.g., A=01, B=02, ..., Z=26).</summary>
    CustomOffset,

    /// <summary>Base-16 (hexadecimal) encoding of each character's code point.</summary>
    Base16,
}
