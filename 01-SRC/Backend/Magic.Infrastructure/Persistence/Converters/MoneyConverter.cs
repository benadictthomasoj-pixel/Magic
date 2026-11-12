using Magic.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Magic.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core ValueConverter mapping pure Domain Money value objects to 64-bit bigint columns in PostgreSQL.
/// </summary>
public sealed class MoneyConverter() : ValueConverter<Money, long>(
    money => money.Paise,
    paise => Money.FromPaise(paise));
