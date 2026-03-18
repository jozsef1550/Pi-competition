# π Search — High-Performance Pi Explorer

> A high-performance **.NET 8 / WPF** application that visualises the search
> for complex strings and formulas within the decimal digits of π.

[![CI](https://github.com/jozsef1550/Pi-competition/actions/workflows/ci.yml/badge.svg)](https://github.com/jozsef1550/Pi-competition/actions/workflows/ci.yml)

---

## Features

| Category | Capability |
|---|---|
| **Encoding** | ASCII/UTF-8 · Custom Offset (A=01…Z=26) · Base-16 (hex) |
| **Search** | Boyer-Moore algorithm (bad-character + good-suffix tables) |
| **Parallelism** | Up to 3 encoding keys race simultaneously via `Task.WhenAll` |
| **Memory** | `MemoryMappedFile` – never loads the entire π file into RAM |
| **Animation** | Matrix-style falling-digit rain; neon-green highlight on match |
| **Drawer** | Animated side-panel showing the live character→number map |
| **Statistics** | Current index · digits/sec velocity · probability meter |
| **Race view** | Side-by-side panels for each encoding key |

### How "1+1=2" is found

```
Input:    1  +  1  =  2
ASCII:   49  43 49  61  50
Pattern: 4943496150
```

The app then searches the digits of π for the continuous string `4943496150`.
If no match is found it can retry with an offset (adding `+1` to every code point).

---

##  Solution Structure

```
PiSearch.slnx
│
├── PiSearch.Core/          # Cross-platform algorithms & services (.NET 8)
│   ├── Algorithms/
│   │   └── BoyerMooreSearcher.cs   – full BM with bad-char & good-suffix
│   ├── Models/
│   │   ├── EncodingMethod.cs       – enum: Ascii | CustomOffset | Base16
│   │   ├── SearchOptions.cs        – per-key search configuration
│   │   ├── SearchResult.cs         – match index, elapsed time, etc.
│   │   └── SearchStatistics.cs     – live velocity / probability snapshot
│   └── Services/
│       ├── EncodingService.cs      – converts text → digit string
│       ├── PiDigitReader.cs        – MemoryMappedFile reader + 1 000-digit fallback
│       ├── SearchService.cs        – multi-threaded chunk search
│       └── StatisticsService.cs    – velocity & P(match) formula
│
├── PiSearch.App/           # WPF .NET 8 desktop application (Windows)
│   ├── Controls/
│   │   ├── MatrixAnimationControl  – falling-digit rain, neon found-overlay
│   │   └── SamplingDrawer          – animated slide-in char-map panel
│   ├── Converters/
│   │   └── ValueConverters.cs      – bool→visibility, velocity, probability, …
│   └── ViewModels/
│       ├── MainViewModel.cs        – search orchestration, command wiring
│       └── SearchKeyViewModel.cs   – per-key live stats + char map
│
├── PiSearch.Tests/         # xUnit test project (49 tests)
│   ├── BoyerMooreSearcherTests.cs
│   ├── EncodingServiceTests.cs
│   ├── SearchServiceTests.cs
│   └── StatisticsServiceTests.cs
│
└── PiSearch.Tools/         # CLI utility: generate a π-digit file
    └── Program.cs          – Machin formula, arbitrary-precision BigInteger
```

---

##  Quick Start

### Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0 (see `global.json`) |
| OS (WPF app) | Windows 10 / 11 |
| OS (Core + Tests) | Windows / macOS / Linux |

### 1 — Clone & restore

```bash
git clone https://github.com/jozsef1550/Pi-competition.git
cd Pi-competition
dotnet restore
```

### 2 — Build

```bash
# Cross-platform core library
dotnet build PiSearch.Core/PiSearch.Core.csproj

# WPF desktop application (Windows only)
dotnet build PiSearch.App/PiSearch.App.csproj
```

### 3 — Run tests

```bash
dotnet test PiSearch.Tests/PiSearch.Tests.csproj
```

All **49 tests** should pass.

### 4 — Generate a π-digit file (optional but recommended)

The app ships with an embedded 1 000-digit fallback; for serious searches
generate a larger file first:

```bash
# 10 000 digits (~10 KB) – fast, suitable for demo
dotnet run --project PiSearch.Tools -- --digits 10000 --output pi.txt

# 1 000 000 digits (~1 MB) – recommended
dotnet run --project PiSearch.Tools -- --digits 1000000 --output pi_1m.txt

# Options
dotnet run --project PiSearch.Tools -- --help
```

For **1 billion+ digits** download a pre-computed file and point the app at it
(see *Using a large π file* below).

### 5 — Run the WPF app

```bash
dotnet run --project PiSearch.App
```

Or build a self-contained single-file executable:

```bash
dotnet publish PiSearch.App/PiSearch.App.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  --output ./publish
```

---

##  Using the App

### Search bar

| Control | Purpose |
|---|---|
| **Text field** | Enter anything: `Hello`, `1+1=2`, `42`, `!"£$%` |
| **ENCODING** | Choose `Ascii`, `CustomOffset`, or `Base16` |
| **OFFSET** | Shift every character code (e.g. `+1`) to try alternative keys |
| **ALTERNATIVES** | Add up to 3 parallel search keys (−/+ buttons) |
| **π FILE** | Path to a plain-text digit file; leave blank for built-in 1 000 digits |
| **▶ SEARCH** | Start all keys simultaneously |
| **■ STOP** | Cancel all running tasks |

### Encoding examples

| Input | Encoding | Offset | Digit string searched |
|---|---|---|---|
| `A!` | ASCII | 0 | `6533` |
| `1+1=2` | ASCII | 0 | `4943496150` |
| `HELLO` | CustomOffset | 0 | `0805121215` |
| `A` | Base16 | 0 | `41` |
| `A` | ASCII | 1 | `66` (shifted) |

### Sampling Drawer

Click **⊞ SAMPLING DRAWER** to slide open the side-panel showing every
character's mapping under the current encoding key.

### Alternative Hits (Race View)

Set **ALTERNATIVES** to 2 or 3. Each key gets its own card showing:

- **Index** – which π decimal place is being checked
- **Speed** – digits examined per second
- **Prob** – P(match) = 1 − (1 − 10⁻ᵐ)ⁿ

The card border turns **neon green** when that key wins the race.

---

##  Using a large π file

The `PiDigitReader` accepts any plain-text file whose content is the decimal
expansion of π (digits only, no spaces):

```
3141592653589793238462643383…
```

Files with the `3.` prefix are also supported (the dot is skipped automatically).

**Recommended sources for 1 billion+ digits:**

| Source | URL |
|---|---|
| y-cruncher | https://www.numberworld.org/y-cruncher/ |
| MIT / π files | https://stuff.mit.edu/afs/sipb/contrib/pi/ |

Point the app at the file via the **π FILE** field or the `…` browse button.

---

##  Algorithm Details

### Boyer-Moore

`BoyerMooreSearcher` builds two shift tables at construction time:

1. **Bad-character table** (256 entries) – shifts past mismatches using the
   last occurrence of the mismatched character in the pattern.
2. **Good-suffix table** – shifts past aligned suffixes that re-appear in
   the pattern.

The algorithm then takes `max(bad-char-shift, good-suffix-shift)` at each
mismatch, giving **sub-linear** average-case performance on long texts.

### Memory-mapped I/O

`PiDigitReader` wraps a `MemoryMappedFile` so the OS streams pages on demand.
`SearchService` reads in 4 MB chunks with a `(patternLength − 1)` overlap so
no match can be missed at a chunk boundary.

### Probability formula

```
P(at least one match) = 1 − (1 − 10⁻ᵐ)ⁿ
```

where **m** = pattern length (digits) and **n** = digits scanned so far.

---

##  Tests

```
dotnet test PiSearch.Tests/PiSearch.Tests.csproj --verbosity normal
```

| Suite | Tests |
|---|---|
| `BoyerMooreSearcherTests` | 12 |
| `EncodingServiceTests` | 20 |
| `SearchServiceTests` | 9 |
| `StatisticsServiceTests` | 8 |
| **Total** | **49** |

---

##  Contributing

1. Fork the repository and create a feature branch.
2. Run tests (`dotnet test`) before opening a PR.
3. Follow the coding conventions in `.editorconfig`.
4. CI will build and test both `PiSearch.Core` (Linux) and `PiSearch.App` (Windows).

---

##  Licence

This project is released for the Pi-Competition and is open to all contributors.

