using System.IO.MemoryMappedFiles;

namespace PiSearch.Core.Services;

/// <summary>
/// Reads digits of π from a plain-text file (one digit per character, no spaces or
/// newlines between digits, e.g. "31415926…") using a
/// <see cref="MemoryMappedFile"/> so that the entire file is never loaded into RAM.
/// </summary>
/// <remarks>
/// Expected file format: a plain UTF-8 / ASCII text file whose content is the
/// decimal expansion of π, optionally prefixed with "3." (the leading "3" and
/// decimal point are skipped automatically if a '.' is found in the first 3 bytes).
/// </remarks>
public sealed class PiDigitReader : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly long _startOffset;   // byte offset to first digit in the file
    private readonly long _totalDigits;
    private bool _disposed;

    // ─── Construction & factory ───────────────────────────────────────────

    /// <param name="filePath">
    /// Full path to the π-digit file. If the file does not exist the reader falls
    /// back to an embedded constant (first 1 000 digits) so that the application
    /// can be exercised without a large data file.
    /// </param>
    public PiDigitReader(string filePath)
    {
        if (File.Exists(filePath))
        {
            _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0,
                MemoryMappedFileAccess.Read);

            // Detect optional "3." prefix
            using var probe = _mmf.CreateViewAccessor(0, Math.Min(4, new FileInfo(filePath).Length),
                MemoryMappedFileAccess.Read);
            byte b0 = probe.ReadByte(0);
            byte b1 = probe.ReadByte(1);
            _startOffset = (b0 == (byte)'3' && b1 == (byte)'.') ? 2 : 0;

            _totalDigits = new FileInfo(filePath).Length - _startOffset;
        }
        else
        {
            // Fallback: use embedded constant digits
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(EmbeddedPiDigits);
            _mmf = MemoryMappedFile.CreateNew(null, bytes.Length);
            using var writer = _mmf.CreateViewAccessor();
            writer.WriteArray(0, bytes, 0, bytes.Length);
            _startOffset = 0;
            _totalDigits = bytes.Length;
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────

    /// <summary>Total number of π digits available from this reader.</summary>
    public long TotalDigits => _totalDigits;

    /// <summary>
    /// Reads up to <paramref name="length"/> digit bytes starting at
    /// <paramref name="digitIndex"/> into <paramref name="buffer"/>.
    /// Returns the number of bytes actually read.
    /// </summary>
    public int ReadDigits(long digitIndex, Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        long fileOffset = _startOffset + digitIndex;
        long available = Math.Max(0L, _totalDigits - digitIndex);
        int toRead = (int)Math.Min(buffer.Length, available);
        if (toRead == 0) return 0;

        using var accessor = _mmf.CreateViewAccessor(fileOffset, toRead,
            MemoryMappedFileAccess.Read);
        byte[] tmp = new byte[toRead];
        accessor.ReadArray(0, tmp, 0, toRead);
        tmp.CopyTo(buffer[..toRead]);
        return toRead;
    }

    /// <summary>
    /// Creates a <see cref="Stream"/> view over the digit file starting at
    /// <paramref name="digitIndex"/>. Caller is responsible for disposing.
    /// </summary>
    public Stream CreateStream(long digitIndex = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        long fileOffset = _startOffset + digitIndex;
        long streamLen = _totalDigits - digitIndex;
        return _mmf.CreateViewStream(fileOffset, streamLen, MemoryMappedFileAccess.Read);
    }

    // ─── IDisposable ─────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            _mmf.Dispose();
            _disposed = true;
        }
    }

    // ─── Embedded fallback digits (first 1 000 decimal places) ───────────

    private const string EmbeddedPiDigits =
        "3141592653589793238462643383279502884197169399375105820974944592307816406286" +
        "2089986280348253421170679821480865132823066470938446095505822317253594081284" +
        "8111745028410270193852110555964462294895493038196442881097566593344612847564" +
        "8233786783165271201909145648566923460348610454326648213393607260249141273724" +
        "5870066063155881748815209209628292540917153643678925903600113305305488204665" +
        "2138414695194151160943305727036575959195309218611738193261179310511854807446" +
        "2379962749567351885752724891227938183011949129833673362440656643086021394946" +
        "3952247371907021798609437027705392171762931767523846748184676694051320005681" +
        "2714526356082778577134275778960917363717872146844090122495343014654958537105" +
        "0792279689258923542019956112129021960864034418159813629774771309960518707211" +
        "3499999837297804995105973173281609631859502445945534690830264252230825334468" +
        "5035261931188171010003137838752886587533208381420617177669147303598253490428" +
        "7554687311595628638823537875937519577818577805321712268066130019278766111959" +
        "0921642019";
}
