using System.Security.Cryptography.X509Certificates;
using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Tests;

/// <summary>
/// BIT STRING construction: the unused-bits byte the constructors derive, and content given to the factory.
/// </summary>
public class BitStringTests
{
    [Theory]
    [InlineData("80", 7)]
    [InlineData("01", 0)]
    [InlineData("A0", 5)]
    [InlineData("06", 1)]
    [InlineData("FF", 0)]
    [InlineData("0080", 7)]
    [InlineData("00", 0)] // a last byte of 0 counts as having no unused bits
    public void CalculateUnusedBits_CountsTrailingZeroBits(string hex, int expected)
    {
        Assert.Equal(expected, BitString.CalculateUnusedBits(Convert.FromHexString(hex)));
    }

    [Theory]
    [InlineData(X509KeyUsageFlags.DigitalSignature)]
    [InlineData(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment)]
    [InlineData(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign)]
    [InlineData(X509KeyUsageFlags.EncipherOnly)]
    [InlineData(X509KeyUsageFlags.DecipherOnly)]
    [InlineData(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.DecipherOnly)]
    public void Constructor_WritesNamedBitsAsDer(X509KeyUsageFlags flags)
    {
        // RawData is BIT STRING: 03, length, unused bits, bits. DER drops the trailing zero bits of a named bit list.
        var der = new X509KeyUsageExtension(flags, false).RawData;

        var content = new BitString(der[3..]).Content;

        Assert.Equal(Convert.ToHexString(der[2..]), Convert.ToHexString(content));
    }

    [Fact]
    public void Constructor_EmptyBits_WritesOnlyTheUnusedBitsByte()
    {
        Assert.Equal([0x00], new BitString(Array.Empty<byte>()).Content);
    }

    [Fact]
    public void Factory_KeepsContentAsGiven()
    {
        // Content handed to a factory already starts with the unused-bits byte; another one used to be put in front.
        var tag = new DefaultTagFactory().Create(0, false, 3, [0x05, 0xA0]);

        Assert.Equal([0x05, 0xA0], tag.Content);
    }
}
