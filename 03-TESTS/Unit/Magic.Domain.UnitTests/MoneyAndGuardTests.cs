using System.Text.Json;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Xunit;

namespace Magic.Domain.UnitTests;

public sealed class MoneyAndGuardTests
{
    private sealed record SampleRaisedEvent(string Code) : DomainEventBase;

    private sealed class SampleEntity : DomainEntity
    {
        public void Trigger(string code) => RaiseDomainEvent(new SampleRaisedEvent(code));
    }

    [Fact]
    public void FromRupeesConvertsExactlyToPaise()
    {
        var amount = Money.FromRupees(250);

        Assert.Equal(25_000L, amount.Paise);
        Assert.Equal("250.00 INR", amount.ToString());
        Assert.True(amount.IsPositive);
    }

    [Fact]
    public void ArithmeticOperatorsPreserveExactPaiseWithoutFloatingPointDrift()
    {
        var itemA = Money.FromPaise(1999);
        var itemB = Money.FromPaise(1);

        var total = itemA + itemB;
        var discount = Money.FromPaise(505);
        var net = total - discount;

        Assert.Equal(2000L, total.Paise);
        Assert.Equal(1495L, net.Paise);
        Assert.Equal("14.95 INR", net.ToString());
        Assert.Equal("5.05 INR", (-discount).Abs().ToString());
    }

    [Fact]
    public void AdditionOverflowThrowsOverflowException()
    {
        var max = Money.FromPaise(long.MaxValue);
        var onePaise = Money.FromPaise(1);

        Assert.Throws<OverflowException>(() => _ = max + onePaise);
    }

    [Fact]
    public void DomainGuardRejectsEmptyGuidBlankStringAndNegativeLong()
    {
        var exGuid = Assert.Throws<DomainException>(() => DomainGuard.NotEmpty(Guid.Empty, "branchId"));
        Assert.Equal("DOMAIN_EMPTY_GUID", exGuid.ErrorCode);

        var exText = Assert.Throws<DomainException>(() => DomainGuard.NotNullOrWhiteSpace("   ", "code"));
        Assert.Equal("DOMAIN_REQUIRED_TEXT", exText.ErrorCode);

        var exNegative = Assert.Throws<DomainException>(() => DomainGuard.NonNegative(-1, "quantity"));
        Assert.Equal("DOMAIN_NEGATIVE_VALUE", exNegative.ErrorCode);
    }

    [Fact]
    public void DomainEntityGeneratesSequentialUuidV7AndTracksDomainEvents()
    {
        var entity = new SampleEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, entity.Id.Version);

        entity.Trigger("CREATED");
        Assert.Single(entity.DomainEvents);

        entity.ClearDomainEvents();
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void MoneySerializesAndDeserializesAsScalarInt64PaiseInJson()
    {
        var amount = Money.FromPaise(12550); // 125.50 INR
        var json = JsonSerializer.Serialize(amount);

        Assert.Equal("12550", json);

        var deserialized = JsonSerializer.Deserialize<Money>(json);
        Assert.Equal(amount, deserialized);
        Assert.Equal(12550L, deserialized.Paise);
    }

    [Fact]
    public void FromRupeesDecimalConvertsWithBankersRoundingByDefault()
    {
        var exact = Money.FromRupees(100.50m);
        Assert.Equal(10050L, exact.Paise);
        Assert.Equal(100.50m, exact.ToRupeesDecimal());

        // Banker's rounding test (ToEven): 100.005 rounds to 100.00 (0 is even), 100.015 rounds to 100.02 (2 is even)
        var roundEven0 = Money.FromRupees(100.005m, MidpointRounding.ToEven);
        Assert.Equal(10000L, roundEven0.Paise);

        var roundEven2 = Money.FromRupees(100.015m, MidpointRounding.ToEven);
        Assert.Equal(10002L, roundEven2.Paise);

        // AwayFromZero test: 100.005 rounds to 100.01
        var roundAway = Money.FromRupees(100.005m, MidpointRounding.AwayFromZero);
        Assert.Equal(10001L, roundAway.Paise);
    }
}

