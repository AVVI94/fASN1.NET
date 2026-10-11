using System.Formats.Asn1;
using System.Globalization;
using System.Numerics;
using fASN1.NET.ContentParsing;
using fASN1.NET.Tags;

namespace fASN1.NET.Tests;

/// <summary>
/// Values written by the INTEGER and UTCTime constructors, compared with what System.Formats.Asn1 writes
/// for the same value, and read back through the library's own parsing strategies.
/// </summary>
public class ValueEncodingTests
{
    public static IEnumerable<object[]> Integers =>
        new BigInteger[]
        {
            0, 1, 127, 128, 255, 256, 32767, 32768, 65535, -1, -127, -128, -129, -256, -32768, -32769,
            int.MaxValue, int.MinValue, uint.MaxValue, long.MaxValue, long.MinValue, ulong.MaxValue,
            BigInteger.Parse("123456789012345678901234567890"), BigInteger.Parse("-123456789012345678901234567890"),
        }.Select(v => new object[] { v.ToString(CultureInfo.InvariantCulture) });

    [Theory]
    [MemberData(nameof(Integers))]
    public void Integer_MatchesDerEncoding(string decimalValue)
    {
        var value = BigInteger.Parse(decimalValue, CultureInfo.InvariantCulture);

        var content = new Integer(value).Content;

        Assert.Equal(Convert.ToHexString(DerIntegerContent(value)), Convert.ToHexString(content));
        Assert.Equal(decimalValue, IntegerParsingStrategy.Default.Parse(content));
    }

    [Fact]
    public void Integer_PrimitiveConstructors_MatchBigInteger()
    {
        Assert.Equal(new Integer((BigInteger)128).Content, new Integer(128).Content);
        Assert.Equal(new Integer((BigInteger)(-129L)).Content, new Integer(-129L).Content);
        Assert.Equal(new Integer((BigInteger)short.MinValue).Content, new Integer(short.MinValue).Content);
        Assert.Equal(new Integer((BigInteger)uint.MaxValue).Content, new Integer(uint.MaxValue).Content);
        Assert.Equal(new Integer((BigInteger)ushort.MaxValue).Content, new Integer(ushort.MaxValue).Content);
        Assert.Equal(new Integer((BigInteger)ulong.MaxValue).Content, new Integer(ulong.MaxValue).Content);
        Assert.Equal([0x00, 0x80], new Integer(128).Content);
        Assert.Equal([0xFF], new Integer(-1).Content);
        Assert.Equal([0x00], new Integer(0).Content);
    }

    public static IEnumerable<object[]> UtcTimes => new[]
    {
        new object[] { "2024-02-29T23:59:58Z", "240229235958Z" },
        new object[] { "1999-12-31T00:00:00Z", "991231000000Z" },
        new object[] { "2000-01-01T00:00:00Z", "000101000000Z" },
        new object[] { "1950-01-01T00:00:00Z", "500101000000Z" },
        new object[] { "1969-07-20T20:17:40Z", "690720201740Z" },
        new object[] { "2049-12-31T23:59:59Z", "491231235959Z" },
    };

    [Theory]
    [MemberData(nameof(UtcTimes))]
    public void UtcTime_WritesUtcWithTwoDigitYear(string isoTime, string expected)
    {
        var time = DateTime.Parse(isoTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

        var content = new UTCTime(time).Content;

        Assert.Equal(expected, System.Text.Encoding.ASCII.GetString(content));
        Assert.Equal(Convert.ToHexString(DerUtcTimeContent(time)), Convert.ToHexString(content));
        Assert.Equal(expected, UtcTimeParsingStrategy.Default.Reverse(time));
    }

    [Theory]
    [MemberData(nameof(UtcTimes))]
    public void UtcTime_ReadsBackTheSameInstant(string isoTime, string _)
    {
        var time = DateTime.Parse(isoTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

        var text = UtcTimeParsingStrategy.Default.Parse(new UTCTime(time).Content);

        var parsed = DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        Assert.Equal(time, parsed);
        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
    }

    [Fact]
    public void UtcTime_ConvertsLocalTimeToUtc()
    {
        var utc = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new UTCTime(utc).Content, new UTCTime(utc.ToLocalTime()).Content);
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2050)]
    public void UtcTime_RejectsYearsItCannotHold(int year)
    {
        var time = new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() => new UTCTime(time));
        Assert.Throws<ArgumentOutOfRangeException>(() => UtcTimeParsingStrategy.Default.Reverse(time));
    }

    [Theory]
    [InlineData("500101000000Z", 1950)]
    [InlineData("691231235959Z", 1969)] // read as 2069 before the RFC 5280 rule
    [InlineData("991231235959Z", 1999)]
    [InlineData("000101000000Z", 2000)]
    [InlineData("491231235959Z", 2049)]
    public void UtcTimeParsing_UsesRfc5280Years(string content, int expectedYear)
    {
        var text = UtcTimeParsingStrategy.Default.Parse(System.Text.Encoding.ASCII.GetBytes(content));

        Assert.Equal(expectedYear, DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).Year);
    }

    private static byte[] DerIntegerContent(BigInteger value)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteInteger(value);
        return ContentOf(writer.Encode());
    }

    private static byte[] DerUtcTimeContent(DateTime utcTime)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteUtcTime(new DateTimeOffset(utcTime, TimeSpan.Zero));
        return ContentOf(writer.Encode());
    }

    private static byte[] ContentOf(byte[] encoded)
    {
        AsnDecoder.ReadEncodedValue(encoded, AsnEncodingRules.DER, out var offset, out var length, out _);
        return encoded.AsSpan(offset, length).ToArray();
    }
}
