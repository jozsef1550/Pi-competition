using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using PiSearch.Core.Models;
using PiSearch.Core.Services;

namespace PiSearch.App.ViewModels;

/// <summary>
/// ViewModel for a single search key slot (one row in the alternative-hits panel).
/// </summary>
public sealed class SearchKeyViewModel : ViewModelBase
{
    private string _label = string.Empty;
    private string _encodedPattern = string.Empty;
    private EncodingMethod _encoding = EncodingMethod.Ascii;
    private int _offset;
    private bool _isRunning;
    private bool _isFound;
    private long _currentIndex;
    private double _velocity;
    private double _probability;
    private string _resultInfo = string.Empty;

    public string Label            { get => _label;          set => SetField(ref _label, value); }
    public string EncodedPattern   { get => _encodedPattern; set => SetField(ref _encodedPattern, value); }
    public EncodingMethod Encoding { get => _encoding;       set => SetField(ref _encoding, value); }
    public int Offset              { get => _offset;         set => SetField(ref _offset, value); }
    public bool IsRunning          { get => _isRunning;      set => SetField(ref _isRunning, value); }
    public bool IsFound            { get => _isFound;        set => SetField(ref _isFound, value); }
    public long CurrentIndex       { get => _currentIndex;   set => SetField(ref _currentIndex, value); }
    public double Velocity         { get => _velocity;       set => SetField(ref _velocity, value); }
    public double Probability      { get => _probability;    set => SetField(ref _probability, value); }
    public string ResultInfo       { get => _resultInfo;     set => SetField(ref _resultInfo, value); }

    // Character-to-number map entries shown in the Sampling Drawer
    public ObservableCollection<CharMapEntry> CharacterMap { get; } = new();

    public void ApplyStatistics(SearchStatistics stats)
    {
        CurrentIndex = stats.CurrentIndex;
        Velocity     = stats.VelocityDigitsPerSecond;
        Probability  = stats.Probability;
    }

    public void ApplyResult(SearchResult? result)
    {
        if (result is null)
        {
            ResultInfo = "Not found";
            IsFound    = false;
        }
        else
        {
            IsFound    = true;
            ResultInfo = $"Found at index {result.Index:N0} after {result.ElapsedTime.TotalSeconds:F2}s";
            CurrentIndex = result.Index;
        }
        IsRunning = false;
    }
}

/// <summary>One row in the Sampling Drawer (character → encoded value).</summary>
public sealed class CharMapEntry
{
    public char Character    { get; init; }
    public string Encoded    { get; init; } = string.Empty;
    public string Description{ get; init; } = string.Empty;
}
