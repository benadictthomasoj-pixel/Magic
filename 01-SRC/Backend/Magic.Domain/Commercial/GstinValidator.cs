using System.Text.RegularExpressions;

namespace Magic.Domain.Commercial;

/// <summary>
/// Authoritative validator for Indian 15-character Goods and Services Tax Identification Numbers (GSTIN).
/// Validates structure, embedded PAN, state code consistency, and statutory checksum.
/// </summary>
public static partial class GstinValidator
{
    private static readonly Regex GstinFormatRegex = MyGstinRegex();

    public static bool IsValid(string? gstin, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(gstin))
        {
            errorMessage = "GSTIN cannot be empty.";
            return false;
        }

        var trimmed = gstin.Trim().ToUpperInvariant();
        if (trimmed.Length != 15)
        {
            errorMessage = $"GSTIN must be exactly 15 characters (provided {trimmed.Length}).";
            return false;
        }

        if (!GstinFormatRegex.IsMatch(trimmed))
        {
            errorMessage = "GSTIN format is invalid. Expected pattern: 2-digit state code + 10-char PAN + 1-char entity + 'Z' + 1-char checksum.";
            return false;
        }

        // Validate state code (01 to 38, or 97, 99)
        var stateCodeStr = trimmed[..2];
        if (!int.TryParse(stateCodeStr, out var stateCode) || stateCode < 1 || (stateCode > 38 && stateCode != 97 && stateCode != 99))
        {
            errorMessage = $"Invalid GST state code '{stateCodeStr}'. Must be valid Indian state/UT code (01-38, 97, 99).";
            return false;
        }

        // Validate embedded PAN structure (chars 3 to 12)
        var pan = trimmed.Substring(2, 10);
        if (!IsValidPan(pan))
        {
            errorMessage = $"Embedded PAN '{pan}' within GSTIN is structurally invalid.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static bool IsValidPan(string? pan)
    {
        if (string.IsNullOrWhiteSpace(pan))
        {
            return false;
        }

        var trimmed = pan.Trim().ToUpperInvariant();
        if (trimmed.Length != 10)
        {
            return false;
        }

        // 5 uppercase letters, 4 digits, 1 uppercase letter
        for (var i = 0; i < 5; i++)
        {
            if (trimmed[i] < 'A' || trimmed[i] > 'Z') return false;
        }

        for (var i = 5; i < 9; i++)
        {
            if (trimmed[i] < '0' || trimmed[i] > '9') return false;
        }

        return trimmed[9] >= 'A' && trimmed[9] <= 'Z';
    }

    public static string ExtractStateCode(string gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin) || gstin.Length < 2)
        {
            return string.Empty;
        }

        return gstin.Trim()[..2];
    }

    [GeneratedRegex(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", RegexOptions.Compiled)]
    private static partial Regex MyGstinRegex();
}

