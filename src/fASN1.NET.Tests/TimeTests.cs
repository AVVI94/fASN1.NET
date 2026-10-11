using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fASN1.NET.ContentParsing;
using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// UTCTime and GeneralizedTime: every form X.680 allows, results that do not depend on the current culture, and
/// malformed content that must not throw.
/// </summary>
public class TimeTests
{
    [Theory]
    [InlineData("250102030405Z", "2025-01-02T03:04:05Z")]
    [InlineData("2501020304Z", "2025-01-02T03:04:00Z")]              // seconds are optional
    [InlineData("250102030405+0100", "2025-01-02T02:04:05Z")]        // an offset is converted to UTC
    [InlineData("250102003000-0130", "2025-01-02T02:00:00Z")]
    [InlineData("500101000000Z", "1950-01-01T00:00:00Z")]
    [InlineData("491231235959Z", "2049-12-31T23:59:59Z")]
    public void UtcTime_IsShownInUtc(string content, string expected)
    {
        Assert.Equal(expected, UtcTimeParsingStrategy.Default.Parse(Ascii(content)));
    }

    [Theory]
    [InlineData("20250102030405Z", "2025-01-02T03:04:05Z")]
    [InlineData("20250102030405.25Z", "2025-01-02T03:04:05.25Z")]
    [InlineData("20250102030405,5Z", "2025-01-02T03:04:05.5Z")]      // a comma is a decimal sign too
    [InlineData("20250102030405.0Z", "2025-01-02T03:04:05Z")]
    [InlineData("2025010203Z", "2025-01-02T03:00:00Z")]              // minutes and seconds are optional
    [InlineData("202501020304.5Z", "2025-01-02T03:04:30Z")]          // the fraction belongs to the last unit
    [InlineData("2025010203.25Z", "2025-01-02T03:15:00Z")]
    [InlineData("20250102030405+0200", "2025-01-02T01:04:05Z")]
    [InlineData("20250102030405-05", "2025-01-02T08:04:05Z")]        // offset minutes are optional
    [InlineData("20250102030405", "2025-01-02T03:04:05")]            // no zone: local time, shown without Z
    public void GeneralizedTime_IsShownAsIso8601(string content, string expected)
    {
        Assert.Equal(expected, GeneralizedTimeParsingStrategy.Default.Parse(Ascii(content)));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("251302030405Z")]   // month 13
    [InlineData("250230030405Z")]   // 30 February
    [InlineData("250102246000Z")]   // hour 24
    [InlineData("250102030405")]    // UTCTime needs a zone
    [InlineData("250102030405+01")] // ... and offset minutes
    [InlineData("250102030405Zx")]
    public void InvalidUtcTime_IsShownAsItsBytesWithoutThrowing(string content)
    {
        Assert.Equal(OctetStringParsingStrategy.Default.Parse(Ascii(content)), UtcTimeParsingStrategy.Default.Parse(Ascii(content)));
    }

    [Theory]
    [InlineData("2025")]
    [InlineData("20250102030405.Z")] // a decimal sign needs digits
    [InlineData("20250102030405+2400")]
    [InlineData("00000102030405Z")]  // year 0
    public void InvalidGeneralizedTime_IsShownAsItsBytesWithoutThrowing(string content)
    {
        Assert.Equal(content, GeneralizedTimeParsingStrategy.Default.Parse(Ascii(content)));
    }

    [Theory]
    [InlineData("th-TH")] // Buddhist calendar: the year would be 2568
    [InlineData("ar-SA")] // Hijri calendar
    [InlineData("cs-CZ")]
    [InlineData("en-US")]
    public void Text_DoesNotDependOnTheCurrentCulture(string culture)
    {
        var time = new DateTime(2025, 1, 2, 3, 4, 5, 250, DateTimeKind.Utc);
        WithCulture(culture, () =>
        {
            Assert.Equal("2025-01-02T03:04:05Z", UtcTimeParsingStrategy.Default.Parse(Ascii("250102030405Z")));
            Assert.Equal("2025-01-02T03:04:05.25Z", GeneralizedTimeParsingStrategy.Default.Parse(Ascii("20250102030405.25Z")));
            Assert.Equal("20250102030405.25Z", Ascii(new GeneralizedTime(time).Content));
            Assert.Equal("250102030405Z", Ascii(new UTCTime(time).Content));
        });
    }

    [Theory]
    [InlineData(0, "20250102030405Z")]
    [InlineData(1, "20250102030405.0000001Z")]
    [InlineData(2_500_000, "20250102030405.25Z")]
    public void GeneralizedTime_WritesDer(long extraTicks, string expected)
    {
        var time = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(extraTicks);

        var content = new GeneralizedTime(time).Content;

        // ".0" for whole seconds and no "Z" for UTC used to be written; DER forbids both.
        Assert.Equal(expected, Ascii(content));
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteGeneralizedTime(new DateTimeOffset(time), omitFractionalSeconds: false);
        Assert.Equal(writer.Encode()[2..], content);
    }

    [Fact]
    public void GeneralizedTime_ConvertsLocalTimeToUtc()
    {
        var utc = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new GeneralizedTime(utc).Content, new GeneralizedTime(utc.ToLocalTime()).Content);
    }

    public static IEnumerable<object[]> CertificateNames => TestCorpus.Names;

    [Theory]
    [MemberData(nameof(CertificateNames))]
    public void CertificateValidity_MatchesDotNetInUtc(string name)
    {
        var data = TestCorpus.Get(name);
        var tag = Asn1Serializer.Deserialize(data);
        if (!tag.IsCertificate())
        {
            return;
        }
#pragma warning disable SYSLIB0057 // the corpus holds raw DER; X509CertificateLoader is .NET 9+
        using var certificate = new X509Certificate2(data);
#pragma warning restore SYSLIB0057

        Assert.True(tag.TryGetCertificateNotBefore(out var notBefore));
        Assert.True(tag.TryGetCertificateNotAfter(out var notAfter));

        Assert.Equal(DateTimeKind.Utc, notBefore.Kind);
        Assert.Equal(DateTimeKind.Utc, notAfter.Kind);
        Assert.Equal(certificate.NotBefore.ToUniversalTime(), notBefore);
        Assert.Equal(certificate.NotAfter.ToUniversalTime(), notAfter);
    }

    [Fact]
    public void CertificateValidity_DoesNotDependOnTheCurrentCulture()
    {
        // The dates used to go through DateTime.ToString and DateTime.Parse in the current culture.
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get("gen:rsa_root_ca"));

        WithCulture("th-TH", () =>
        {
            Assert.True(tag.TryGetCertificateNotAfter(out var notAfter));
            Assert.Equal(new DateTime(2051, 6, 7, 8, 9, 10, DateTimeKind.Utc), notAfter);
        });
    }

    [Theory]
    [InlineData("1703616263")]            // UTCTime "abc"
    [InlineData("0602FFFF")]              // OBJECT IDENTIFIER with an unterminated arc
    [InlineData("030109")]                // BIT STRING claiming 9 unused bits
    [InlineData("0300")]                  // BIT STRING without the unused-bits byte
    [InlineData("0200")]                  // empty INTEGER
    [InlineData("1801FF")]                // GeneralizedTime with a non-ASCII byte
    public void TagToString_MalformedValue_DoesNotThrow(string hex)
    {
        var tag = Asn1Serializer.Deserialize(Convert.FromHexString(hex));

        var text = Asn1Serializer.TagToString(new Sequence([tag]));

        Assert.StartsWith("Sequence", text);
    }

    [Fact]
    public void TagToString_FailingCustomStrategy_ShowsHex()
    {
        var locator = new StrategyLocator();
        locator.Strategies[(int)Tag.Integer] = new ThrowingStrategy();

        var text = Asn1Serializer.TagToString(new Integer(258), o => o.ContentParsingStrategyLocator = locator);

        Assert.Contains("0102", text);
    }

    private sealed class ThrowingStrategy : IContentParsingStrategy
    {
        public string Parse(byte[] content) => throw new FormatException("custom strategy failure");
    }

    private static byte[] Ascii(string text) => System.Text.Encoding.ASCII.GetBytes(text);

    private static string Ascii(byte[] bytes) => System.Text.Encoding.ASCII.GetString(bytes);

    private static void WithCulture(string name, Action action)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
