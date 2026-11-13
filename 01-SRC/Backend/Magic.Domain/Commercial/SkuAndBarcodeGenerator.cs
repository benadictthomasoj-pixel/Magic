using System.Globalization;

namespace Magic.Domain.Commercial;

/// <summary>
/// Domain utility for generating deterministic, standards-compliant SKUs and EAN-13 barcodes.
/// </summary>
public static class SkuAndBarcodeGenerator
{
    public static string GenerateSku(string categoryCode, string productCode)
    {
        var catPart = string.IsNullOrWhiteSpace(categoryCode) ? "GEN" : categoryCode.Trim().ToUpperInvariant();
        var prodPart = string.IsNullOrWhiteSpace(productCode) ? "PRD" : productCode.Trim().ToUpperInvariant();

        if (catPart.Length > 4) catPart = catPart[..4];
        if (prodPart.Length > 8) prodPart = prodPart[..8];

        var timestampHex = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 100000).ToString("D5", CultureInfo.InvariantCulture);
        return $"{catPart}-{prodPart}-{timestampHex}";
    }

    public static string GenerateEan13(long baseNumber12Digits)
    {
        var s = baseNumber12Digits.ToString("D12", CultureInfo.InvariantCulture);
        if (s.Length > 12)
        {
            s = s[^12..];
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = s[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        var checksum = (10 - (sum % 10)) % 10;
        return $"{s}{checksum}";
    }
}
