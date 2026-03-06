using PiSearch.Core.Models;

namespace PiSearch.Core.Services;

/// <summary>
/// Computes live search statistics including velocity and probability estimates.
/// </summary>
public sealed class StatisticsService
{
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    private long _lastSampleDigits;
    private DateTime _lastSampleTime = DateTime.UtcNow;
    private double _velocity;

    /// <summary>Starts (or restarts) the internal stopwatch.</summary>
    public void Start()
    {
        _stopwatch.Restart();
        _lastSampleDigits = 0;
        _lastSampleTime = DateTime.UtcNow;
        _velocity = 0;
    }

    /// <summary>Stops the internal stopwatch.</summary>
    public void Stop() => _stopwatch.Stop();

    /// <summary>
    /// Builds a <see cref="SearchStatistics"/> snapshot for the given state.
    /// </summary>
    /// <param name="currentIndex">Current π digit index.</param>
    /// <param name="patternLength">Length in digits of the search pattern.</param>
    public SearchStatistics Sample(long currentIndex, int patternLength)
    {
        var now = DateTime.UtcNow;
        double elapsedSec = (now - _lastSampleTime).TotalSeconds;

        if (elapsedSec >= 0.25) // update velocity at most every 250 ms
        {
            long delta = currentIndex - _lastSampleDigits;
            _velocity = delta / elapsedSec;
            _lastSampleDigits = currentIndex;
            _lastSampleTime = now;
        }

        return new SearchStatistics
        {
            CurrentIndex = currentIndex,
            DigitsProcessed = currentIndex,
            VelocityDigitsPerSecond = _velocity,
            Probability = ComputeProbability(currentIndex, patternLength),
            PatternLength = patternLength,
            ElapsedTime = _stopwatch.Elapsed,
        };
    }

    // ─── Probability formula ──────────────────────────────────────────────

    /// <summary>
    /// Approximate probability that a uniformly random pattern of
    /// <paramref name="patternLength"/> decimal digits appears at least once
    /// in the first <paramref name="digitsScanned"/> digits of π.
    ///
    /// P ≈ 1 – (1 – 10^−m)^(n – m + 1)
    /// where m = pattern length, n = digits scanned.
    /// </summary>
    public static double ComputeProbability(long digitsScanned, int patternLength)
    {
        if (patternLength <= 0 || digitsScanned <= 0)
            return 0.0;

        // Probability of a single window matching
        double pSingle = Math.Pow(10.0, -patternLength);
        long windows = Math.Max(0, digitsScanned - patternLength + 1);
        if (windows <= 0) return 0.0;

        // Use log to avoid underflow: 1 – exp(windows * log(1 – pSingle))
        double logNoMatch = windows * Math.Log(1.0 - pSingle);
        return 1.0 - Math.Exp(logNoMatch);
    }
}
