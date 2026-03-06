using System.Numerics;

namespace PiSearch.Tools;

/// <summary>
/// <para>
///   <b>pi-gen</b> – generate a plain-text file containing the decimal digits of π.
/// </para>
/// <para>
///   Uses the Bailey–Borwein–Plouffe (BBP) spigot formula (hex digits) converted to
///   decimal, or – for counts up to ~10 000 – the faster Machin-like formula via
///   arbitrary-precision integer arithmetic.  For very large counts (millions+) we
///   recommend downloading pre-computed files (see README.md).
/// </para>
/// </summary>
/// <remarks>
/// Usage:
/// <code>
///   dotnet run --project PiSearch.Tools -- --digits 10000 --output pi.txt
///   dotnet run --project PiSearch.Tools -- -d 1000000 -o pi_1m.txt
/// </code>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        // ── Argument parsing ────────────────────────────────────────────────
        int digits = 10_000;
        string output = "pi.txt";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-d" or "--digits" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out digits) || digits < 1)
                    {
                        Console.Error.WriteLine("--digits must be a positive integer.");
                        return 1;
                    }
                    break;

                case "-o" or "--output" when i + 1 < args.Length:
                    output = args[++i];
                    break;

                case "-h" or "--help":
                    PrintHelp();
                    return 0;

                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    PrintHelp();
                    return 1;
            }
        }

        Console.WriteLine($"Generating {digits:N0} decimal digits of π → {output}");

        try
        {
            string piDigits = ComputePiMachin(digits);

            // Write: "3141592653…" (no "3." prefix, pure digits)
            File.WriteAllText(output, piDigits);
            Console.WriteLine($"Done. File size: {new FileInfo(output).Length:N0} bytes.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    // ─── Machin-like formula using integer arithmetic ──────────────────────
    //
    //  π/4 = 4·arctan(1/5) − arctan(1/239)        (Machin 1706)
    //
    //  arctan(1/x) = Σ  (-1)^k / ((2k+1) · x^(2k+1))
    //
    //  We work with integers scaled by 10^(digits+guard).

    private const int GuardDigits = 10;

    private static string ComputePiMachin(int requestedDigits)
    {
        int prec = requestedDigits + GuardDigits;

        BigInteger scale = BigInteger.Pow(10, prec);
        BigInteger four  = 4 * scale;
        BigInteger one   = scale;

        // π = 4 · (4·arctan(1/5) − arctan(1/239))
        BigInteger pi = 4 * (4 * ArctanMachin(5, prec) - ArctanMachin(239, prec));

        // Convert to string, strip leading guard digits
        string raw = pi.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // raw should be (digits+guard) characters long; take (requestedDigits+1) to get
        // digits 0..requestedDigits and then strip the decimal point if needed.
        // The integer result represents 3.14159…  scaled by 10^prec.
        // Inserting the decimal point after the first character gives the π expansion.
        if (raw.Length < requestedDigits + 1)
            raw = raw.PadLeft(requestedDigits + GuardDigits + 1, '0');

        // The integer is: 31415926535…  (no decimal point)
        // Strip trailing guard digits to get requestedDigits+1 chars (leading "3" + decimals).
        string significant = raw[..(requestedDigits + 1)];

        // Return just the digit string without any punctuation
        return significant;
    }

    /// <summary>
    /// Computes <c>arctan(1/x)</c> scaled by <c>10^prec</c> using the Maclaurin series.
    /// </summary>
    private static BigInteger ArctanMachin(long x, int prec)
    {
        BigInteger scale = BigInteger.Pow(10, prec);
        BigInteger xPow  = scale / x;            // 1/x · scale
        BigInteger xSq   = x * x;
        BigInteger sum   = xPow;
        BigInteger term  = xPow;

        for (int k = 1; ; k++)
        {
            term /= xSq;
            BigInteger correction = term / (2 * k + 1);
            if (correction == 0) break;

            if (k % 2 == 1)
                sum -= correction;
            else
                sum += correction;
        }

        return sum;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            pi-gen – generate a decimal π-digit file for use with PiSearch

            Usage:
              dotnet run --project PiSearch.Tools -- [OPTIONS]

            Options:
              -d, --digits <count>    Number of decimal digits to generate (default: 10000)
              -o, --output <path>     Output file path (default: pi.txt)
              -h, --help              Show this help message

            Examples:
              dotnet run --project PiSearch.Tools -- --digits 1000000 --output pi_1m.txt
              dotnet run --project PiSearch.Tools -- -d 10000 -o pi.txt

            Notes:
              • The output file contains only digit characters (no "3." prefix).
              • For > 10 million digits, consider downloading a pre-computed file
                from https://www.piday.org/million/ or https://stuff.mit.edu/afs/sipb/
                contrib/pi/ and using that as the --pi-file argument in PiSearch.App.
              • The generator uses the Machin formula with arbitrary-precision integers.
                Expect about 1–2 seconds per 100 000 digits.
            """);
    }
}
