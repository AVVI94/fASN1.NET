using System.Formats.Asn1;
using fASN1.NET.ContentParsing;
using fASN1.NET.Oid;

namespace fASN1.NET.Tests;

/// <summary>
/// OBJECT IDENTIFIER encoding and decoding, compared with System.Formats.Asn1.
/// </summary>
public class OidEncoderTests
{
    [Fact]
    public void EveryRegisteredOid_RoundTrips()
    {
        foreach (var oid in OID.OidDictionary)
        {
            var bytes = OidEncoder.GetBytes(oid.Value.Value);

            Assert.Equal(oid.Key, OidEncoder.GetString(bytes));
            Assert.Equal(DerContent(oid.Key), bytes);
        }
    }

    [Theory]
    [InlineData("0.0")]
    [InlineData("0.39")]
    [InlineData("1.39")]
    [InlineData("2.40")]
    [InlineData("2.47")]
    [InlineData("2.48")]          // first subidentifier 128: two bytes
    [InlineData("2.999")]         // first subidentifier 1079: two bytes; used to decode as 2.56.55
    [InlineData("2.999.3")]
    [InlineData("1.2.840.113549.1.1.11")]
    [InlineData("1.3.6.1.4.1.311.21.20")]
    [InlineData("1.2.4294967295")]
    [InlineData("1.2.18446744073709551615")]  // ulong.MaxValue
    [InlineData("1.2.18446744073709551616")]  // one more
    [InlineData("2.25.329800735698586629295641978511506172918")] // a UUID arc; used to overflow int
    [InlineData("2.18446744073709551700")]
    public void ValidOid_MatchesDotNet(string oid)
    {
        var bytes = OidEncoder.GetBytes(oid);

        Assert.Equal(DerContent(oid), bytes);
        Assert.Equal(oid, OidEncoder.GetString(bytes));
        Assert.True(OidEncoder.TryGetString(bytes, out var text));
        Assert.Equal(oid, text);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("3.1")]      // only 0, 1 and 2 are first arcs
    [InlineData("1.40")]     // under 0 and 1, second arcs stop at 39
    [InlineData("0.40")]
    [InlineData("1.-2")]
    [InlineData("1.+2")]
    [InlineData("1.2.a")]
    [InlineData("1. 2")]
    [InlineData("1..2")]
    [InlineData("1.2.")]
    [InlineData("1.02")]     // leading zero
    [InlineData("1.2.٣")]    // a non-ASCII digit
    public void InvalidOidText_IsRejected(string oid)
    {
        Assert.False(OidEncoder.TryGetBytes(oid, out _));
        Assert.Throws<ArgumentException>(() => OidEncoder.GetBytes(oid));
    }

    [Theory]
    [InlineData("88")]       // unterminated first arc
    [InlineData("2A86")]     // unterminated last arc; used to be dropped silently
    [InlineData("2A8001")]   // leading 0x80 group
    [InlineData("8001")]
    public void InvalidOidBytes_AreRejected(string hex)
    {
        var bytes = Convert.FromHexString(hex);

        Assert.False(OidEncoder.TryGetString(bytes, out _));
        Assert.Throws<ArgumentException>(() => OidEncoder.GetString(bytes));
        Assert.Equal(hex, OidParsingStrategy.Default.Parse(bytes)); // shown as hex, not as a wrong OID
    }

    [Fact]
    public void RandomBytes_AgreeWithDotNet()
    {
        var random = new Random(20251010);
        for (int i = 0; i < 20_000; i++)
        {
            var bytes = new byte[random.Next(1, 24)];
            random.NextBytes(bytes);
            // Bias towards realistic input: mostly single-byte arcs, sometimes long ones.
            for (int j = 0; j < bytes.Length; j++)
            {
                if (random.Next(3) != 0)
                    bytes[j] &= 0x7F;
            }

            string? expected;
            try
            {
                expected = AsnDecoder.ReadObjectIdentifier([0x06, (byte)bytes.Length, .. bytes], AsnEncodingRules.DER, out _);
            }
            catch (AsnContentException)
            {
                expected = null;
            }

            var ok = OidEncoder.TryGetString(bytes, out var actual);

            Assert.True(expected == actual, $"{Convert.ToHexString(bytes)}: expected {expected ?? "invalid"}, got {(ok ? actual : "invalid")}");
        }
    }

    private static byte[] DerContent(string oid)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteObjectIdentifier(oid);
        var encoded = writer.Encode();
        AsnDecoder.ReadEncodedValue(encoded, AsnEncodingRules.DER, out var offset, out var length, out _);
        return encoded.AsSpan(offset, length).ToArray();
    }
}
