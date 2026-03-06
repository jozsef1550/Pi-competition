using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using PiSearch.Core.Models;
using PiSearch.Core.Services;

namespace PiSearch.App.ViewModels;

/// <summary>
/// Primary ViewModel for MainWindow.
/// Drives the search, statistics, animation feed, and all UI panels.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    // ─── Injected services ────────────────────────────────────────────────
    private readonly EncodingService _encodingService = new();
    private readonly Dispatcher _dispatcher;

    // ─── Backing fields ───────────────────────────────────────────────────
    private string _searchText       = string.Empty;
    private string _piFilePath       = string.Empty;
    private EncodingMethod _encoding = EncodingMethod.Ascii;
    private int _offset;
    private bool _isSearching;
    private bool _isDrawerOpen;
    private string _statusMessage   = "Ready";
    private int _alternativeCount   = 1;

    // Matrix animation feed
    private string _matrixFeed = string.Empty;
    private long _highlightIndex = -1;
    private int _highlightLength;

    private CancellationTokenSource? _cts;
    private SearchService? _activeService;

    // ─── Construction ─────────────────────────────────────────────────────

    public MainViewModel(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        SearchKeys.Add(new SearchKeyViewModel
        {
            Label = "Key 1 – ASCII",
            Encoding = EncodingMethod.Ascii,
        });

        StartSearchCommand  = new RelayCommand(StartSearch,  () => !IsSearching && !string.IsNullOrWhiteSpace(SearchText));
        StopSearchCommand   = new RelayCommand(StopSearch,   () => IsSearching);
        ToggleDrawerCommand = new RelayCommand(ToggleDrawer);
        AddKeyCommand       = new RelayCommand(AddKey,       () => SearchKeys.Count < 3);
        RemoveKeyCommand    = new RelayCommand(RemoveKey,    () => SearchKeys.Count > 1);
        BrowsePiFileCommand = new RelayCommand(BrowsePiFile);
    }

    // ─── Bound properties ─────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
                RefreshCharacterMaps();
        }
    }

    public string PiFilePath
    {
        get => _piFilePath;
        set => SetField(ref _piFilePath, value);
    }

    public EncodingMethod Encoding
    {
        get => _encoding;
        set
        {
            if (SetField(ref _encoding, value))
            {
                if (SearchKeys.Count > 0)
                {
                    SearchKeys[0].Encoding = value;
                    RefreshCharacterMaps();
                }
            }
        }
    }

    public int Offset
    {
        get => _offset;
        set
        {
            if (SetField(ref _offset, value))
                RefreshCharacterMaps();
        }
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set => SetField(ref _isSearching, value);
    }

    public bool IsDrawerOpen
    {
        get => _isDrawerOpen;
        set => SetField(ref _isDrawerOpen, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public int AlternativeCount
    {
        get => _alternativeCount;
        set
        {
            if (SetField(ref _alternativeCount, Math.Clamp(value, 1, 3)))
                SyncSearchKeys();
        }
    }

    public string MatrixFeed
    {
        get => _matrixFeed;
        set => SetField(ref _matrixFeed, value);
    }

    public long HighlightIndex
    {
        get => _highlightIndex;
        set => SetField(ref _highlightIndex, value);
    }

    public int HighlightLength
    {
        get => _highlightLength;
        set => SetField(ref _highlightLength, value);
    }

    // ─── Collections ──────────────────────────────────────────────────────

    public ObservableCollection<SearchKeyViewModel> SearchKeys { get; } = new();

    public IEnumerable<EncodingMethod> EncodingMethods
        => Enum.GetValues<EncodingMethod>();

    // ─── Commands ─────────────────────────────────────────────────────────

    public RelayCommand StartSearchCommand  { get; }
    public RelayCommand StopSearchCommand   { get; }
    public RelayCommand ToggleDrawerCommand { get; }
    public RelayCommand AddKeyCommand       { get; }
    public RelayCommand RemoveKeyCommand    { get; }
    public RelayCommand BrowsePiFileCommand { get; }

    // ─── Command implementations ──────────────────────────────────────────

    private async void StartSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        IsSearching = true;
        StatusMessage = "Searching…";
        HighlightIndex = -1;

        foreach (var key in SearchKeys)
        {
            key.IsRunning = true;
            key.IsFound   = false;
            key.ResultInfo = string.Empty;
        }

        _cts = new CancellationTokenSource();

        try
        {
            var reader  = new PiDigitReader(PiFilePath);
            _activeService = new SearchService(reader);

            _activeService.StatisticsUpdated += OnStatisticsUpdated;

            var optionsList = SearchKeys.Select(k => new SearchOptions
            {
                SearchText = SearchText,
                Encoding   = k.Encoding,
                Offset     = k.Offset,
                Label      = k.Label,
            }).ToList();

            // Run all keys and collect results
            var results = await _activeService.SearchAllKeysAsync(optionsList, _cts.Token);

            // Apply results back to each key ViewModel
            foreach (var key in SearchKeys)
            {
                var match = results.FirstOrDefault(r => r.Label == key.Label);
                _dispatcher.Invoke(() => key.ApplyResult(match));
            }

            if (results.Count > 0)
            {
                var best = results[0];
                _dispatcher.Invoke(() =>
                {
                    HighlightIndex  = best.Index;
                    HighlightLength = best.MatchedPattern.Length;
                    StatusMessage   = $"Found! Index {best.Index:N0} ({best.Label})";
                });
            }
            else
            {
                _dispatcher.Invoke(() => StatusMessage = "Pattern not found in available digits.");
            }
        }
        catch (OperationCanceledException)
        {
            _dispatcher.Invoke(() => StatusMessage = "Search cancelled.");
        }
        catch (Exception ex)
        {
            _dispatcher.Invoke(() => StatusMessage = $"Error: {ex.Message}");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _activeService?.Dispose();
            _activeService = null;

            _dispatcher.Invoke(() =>
            {
                IsSearching = false;
                foreach (var key in SearchKeys)
                    key.IsRunning = false;
            });
        }
    }

    private void StopSearch()
    {
        _cts?.Cancel();
        StatusMessage = "Stopping…";
    }

    private void ToggleDrawer() => IsDrawerOpen = !IsDrawerOpen;

    private void AddKey()
    {
        if (SearchKeys.Count >= 3) return;
        var newKey = new SearchKeyViewModel
        {
            Label    = $"Key {SearchKeys.Count + 1} – {(EncodingMethod)(SearchKeys.Count % 3)}",
            Encoding = (EncodingMethod)(SearchKeys.Count % 3),
            Offset   = SearchKeys.Count,
        };
        SearchKeys.Add(newKey);
        RefreshCharacterMapsForKey(newKey);
    }

    private void RemoveKey()
    {
        if (SearchKeys.Count <= 1) return;
        SearchKeys.RemoveAt(SearchKeys.Count - 1);
    }

    private void BrowsePiFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Select π digits file",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog() == true)
            PiFilePath = dlg.FileName;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private void OnStatisticsUpdated(object? sender, SearchStatistics stats)
    {
        _dispatcher.InvokeAsync(() =>
        {
            // Update the first matching key (label-based matching not available here,
            // so update all running keys with the same statistics snapshot)
            foreach (var key in SearchKeys.Where(k => k.IsRunning))
                key.ApplyStatistics(stats);

            StatusMessage = $"Scanning… index {stats.CurrentIndex:N0}  |  " +
                            $"{stats.VelocityDigitsPerSecond / 1_000_000.0:F1} M digits/s";
        });
    }

    private void RefreshCharacterMaps()
    {
        foreach (var key in SearchKeys)
            RefreshCharacterMapsForKey(key);
    }

    private void RefreshCharacterMapsForKey(SearchKeyViewModel key)
    {
        key.CharacterMap.Clear();
        if (string.IsNullOrEmpty(SearchText)) return;

        foreach (var (ch, encoded) in _encodingService.GetCharacterMap(SearchText, key.Encoding, key.Offset))
        {
            key.CharacterMap.Add(new CharMapEntry
            {
                Character   = ch,
                Encoded     = encoded,
                Description = $"'{ch}'  →  {encoded}",
            });
        }

        key.EncodedPattern = _encodingService.Encode(SearchText, key.Encoding, key.Offset);
    }

    private void SyncSearchKeys()
    {
        while (SearchKeys.Count < AlternativeCount)
            AddKey();
        while (SearchKeys.Count > AlternativeCount)
            SearchKeys.RemoveAt(SearchKeys.Count - 1);
    }
}
