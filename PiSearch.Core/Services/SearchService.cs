using System.Diagnostics;
using PiSearch.Core.Algorithms;
using PiSearch.Core.Models;

namespace PiSearch.Core.Services;

/// <summary>
/// Orchestrates multi-threaded searches of π using Boyer-Moore and a memory-mapped
/// digit file. Supports up to N simultaneous searches with different encoding keys.
/// </summary>
public sealed class SearchService : IDisposable
{
    // Buffer size per chunk read from the π file (4 MB). We keep a small overlap
    // equal to the pattern length so we never miss a match that straddles two chunks.
    private const int ChunkSize = 4 * 1024 * 1024;

    private readonly PiDigitReader _reader;
    private bool _disposed;

    public SearchService(PiDigitReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    // ─── Events ───────────────────────────────────────────────────────────

    /// <summary>
    /// Raised on the thread-pool whenever a statistics snapshot is ready.
    /// Subscribe on the UI thread via a dispatcher marshal.
    /// </summary>
    public event EventHandler<SearchStatistics>? StatisticsUpdated;

    // ─── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Runs one or more searches in parallel, each with its own
    /// <see cref="SearchOptions"/>. Returns the first result found, or
    /// <c>null</c> if the pattern is not present within the available digits.
    /// </summary>
    /// <param name="options">
    /// Collection of search configurations (e.g., different encoding keys).
    /// Each is executed on its own task.
    /// </param>
    /// <param name="cancellationToken">Token to cancel all running searches.</param>
    public async Task<SearchResult?> SearchParallelAsync(
        IEnumerable<SearchOptions> options,
        CancellationToken cancellationToken = default)
    {
        var optList = options.ToList();
        if (optList.Count == 0) return null;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var tasks = optList.Select(opt =>
            Task.Run(() => SearchSingle(opt, cts.Token), cts.Token))
            .ToArray();

        // Return the first non-null result; cancel the rest.
        while (tasks.Length > 0)
        {
            var finished = await Task.WhenAny(tasks);
            var result = await finished;
            if (result is not null)
            {
                cts.Cancel(); // stop remaining searches
                return result;
            }
            tasks = tasks.Where(t => t != finished).ToArray();
        }
        return null;
    }

    /// <summary>
    /// Runs all searches in parallel and returns ALL results found
    /// (one per encoding that produced a match), sorted by index.
    /// Useful for the "alternative hits" side-by-side view.
    /// </summary>
    public async Task<IReadOnlyList<SearchResult>> SearchAllKeysAsync(
        IEnumerable<SearchOptions> options,
        CancellationToken cancellationToken = default)
    {
        var tasks = options
            .Select(opt => Task.Run(() => SearchSingle(opt, cancellationToken), cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        return results
            .OfType<SearchResult>()
            .OrderBy(r => r.Index)
            .ToList();
    }

    // ─── Core single-key search ───────────────────────────────────────────

    /// <summary>
    /// Searches π for the pattern derived from <paramref name="options"/>.
    /// Reads the digit file in overlapping chunks so the entire dataset is never
    /// in memory at once.
    /// </summary>
    public SearchResult? SearchSingle(SearchOptions options,
        CancellationToken cancellationToken = default)
    {
        string encoded = EncodingService.Encode(options.SearchText, options.Encoding, options.Offset);
        if (string.IsNullOrEmpty(encoded)) return null;

        var bm = new BoyerMooreSearcher(encoded);
        var stats = new StatisticsService();
        stats.Start();

        var sw = Stopwatch.StartNew();
        int patLen = encoded.Length;

        // Overlap = pattern.Length – 1 so a match straddling two chunks is not missed.
        int overlap = patLen - 1;
        int bufSize = ChunkSize + overlap;
        byte[] buffer = new byte[bufSize];

        long maxDigits = options.MaxDigits > 0
            ? Math.Min(options.MaxDigits, _reader.TotalDigits)
            : _reader.TotalDigits;

        long chunkStart = 0;
        long statsInterval = 500_000; // emit stats every 500k digits
        long nextStatAt = statsInterval;

        while (chunkStart < maxDigits)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long toRead = Math.Min(bufSize, maxDigits - chunkStart);
            int read = _reader.ReadDigits(chunkStart, buffer.AsSpan(0, (int)toRead));
            if (read == 0) break;

            int matchOffset = bm.Search(buffer.AsSpan(0, read));
            if (matchOffset >= 0)
            {
                long absoluteIndex = chunkStart + matchOffset;
                stats.Stop();
                return new SearchResult
                {
                    Index = absoluteIndex,
                    MatchedPattern = encoded,
                    OriginalText = options.SearchText,
                    Encoding = options.Encoding,
                    Offset = options.Offset,
                    DigitsScanned = absoluteIndex + patLen,
                    ElapsedTime = sw.Elapsed,
                    Label = options.Label,
                };
            }

            // Advance by ChunkSize but keep the overlap region for the next iteration.
            chunkStart += Math.Max(1, read - overlap);

            // Emit statistics
            if (chunkStart >= nextStatAt)
            {
                var snap = stats.Sample(chunkStart, patLen);
                StatisticsUpdated?.Invoke(this, snap);
                nextStatAt += statsInterval;
            }
        }

        stats.Stop();
        return null;
    }

    // ─── IDisposable ─────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            _reader.Dispose();
            _disposed = true;
        }
    }
}
