namespace PiSearch.Core.Models;

/// <summary>
/// A snapshot of live search statistics emitted periodically during a running search.
/// </summary>
public sealed class SearchStatistics
{
    /// <summary>Index of the π digit currently being examined.</summary>
    public long CurrentIndex { get; set; }

    /// <summary>Total digits processed since the search started.</summary>
    public long DigitsProcessed { get; set; }

    /// <summary>Digits examined per second (rolling average).</summary>
    public double VelocityDigitsPerSecond { get; set; }

    /// <summary>
    /// Approximate probability that a random sequence of the same length appears
    /// at least once in the number of digits scanned so far.
    /// </summary>
    public double Probability { get; set; }

    /// <summary>Length of the pattern (in digits) being searched.</summary>
    public int PatternLength { get; set; }

    /// <summary>Elapsed time since the search started.</summary>
    public TimeSpan ElapsedTime { get; set; }
}
