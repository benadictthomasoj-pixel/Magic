using System.Text.Json;
using System.Text.Json.Serialization;

namespace Magic.Domain.ValueObjects;

/// <summary>
/// Immutable 64-bit integer monetary value stored strictly in paise (1 INR = 100 paise).
/// Eliminates IEEE-754 binary floating-point rounding drift across all financial operations.
/// </summary>
[JsonConverter(typeof(MoneyJsonConverter))]
public readonly record struct Money(long Paise) : IComparable<Money>
{
    public const long PaisePerRupee = 100L;

    public static Money Zero => new(0L);

    public bool IsZero => Paise == 0L;
    public bool IsPositive => Paise > 0L;
    public bool IsNegative => Paise < 0L;

    public static Money FromPaise(long paise) => new(paise);

    public static Money FromRupees(long wholeRupees) => new(checked(wholeRupees * PaisePerRupee));

    public static Money FromRupees(decimal rupees, MidpointRounding rounding = MidpointRounding.AwayFromZero)
    {
        var paiseDecimal = Math.Round(rupees * PaisePerRupee, 0, rounding);
        return new(checked((long)paiseDecimal));
    }

    public decimal ToRupeesDecimal() => (decimal)Paise / PaisePerRupee;

    public Money Abs() => new(Paise == long.MinValue
        ? throw new OverflowException("Cannot negate Int64.MinValue paise.")
        : Math.Abs(Paise));

    public static Money operator +(Money left, Money right) => new(checked(left.Paise + right.Paise));

    public static Money operator -(Money left, Money right) => new(checked(left.Paise - right.Paise));

    public static Money operator -(Money value) => new(checked(-value.Paise));

    public static bool operator <(Money left, Money right) => left.Paise < right.Paise;

    public static bool operator >(Money left, Money right) => left.Paise > right.Paise;

    public static bool operator <=(Money left, Money right) => left.Paise <= right.Paise;

    public static bool operator >=(Money left, Money right) => left.Paise >= right.Paise;

    public int CompareTo(Money other) => Paise.CompareTo(other.Paise);

    public override string ToString()
    {
        var sign = Paise < 0 ? "-" : string.Empty;
        var abs = Paise == long.MinValue ? (ulong)long.MaxValue + 1UL : (ulong)Math.Abs(Paise);
        var rupees = abs / (ulong)PaisePerRupee;
        var remainder = abs % (ulong)PaisePerRupee;
        return $"{sign}{rupees}.{remainder:D2} INR";
    }
}

/// <summary>
/// Serializes Money as scalar 64-bit integer paise to conform with API contracts and eliminate floating-point objects.
/// </summary>
public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var paise))
        {
            return Money.FromPaise(paise);
        }

        if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), out var parsedPaise))
        {
            return Money.FromPaise(parsedPaise);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Legacy / fallback object shape: {"paise": 10000}
            long extractedPaise = 0;
            var found = false;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName &&
                    string.Equals(reader.GetString(), "paise", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    extractedPaise = reader.GetInt64();
                    found = true;
                }
            }

            if (found)
            {
                return Money.FromPaise(extractedPaise);
            }
        }

        throw new JsonException($"Unable to deserialize monetary value from token type '{reader.TokenType}'. Expected 64-bit integer paise.");
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Paise);
    }
}

