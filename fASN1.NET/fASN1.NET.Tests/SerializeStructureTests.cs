using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Byte-exact checks of <see cref="Asn1Serializer.Serialize"/>: length encoding at every size boundary,
/// and the definite-length output it always produces.
/// </summary>
public class SerializeStructureTests
{
    private static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    [Theory]
    [InlineData(0, "00")]
    [InlineData(127, "7F")]
    [InlineData(128, "81 80")]
    [InlineData(255, "81 FF")]
    [InlineData(256, "82 01 00")]
    [InlineData(65535, "82 FF FF")]
    [InlineData(65536, "83 01 00 00")]
    [InlineData(65537, "83 01 00 01")]
    [InlineData(70000, "83 01 11 70")]
    [InlineData(16777215, "83 FF FF FF")]
    [InlineData(16777216, "84 01 00 00 00")]
    [InlineData(16777473, "84 01 00 01 01")]
    public void Length_IsEncodedMinimallyBigEndian(int length, string expectedLength)
    {
        // 0xFF content never parses as nested elements, so the OCTET STRING stays a leaf on the way back.
        var content = new byte[length];
        content.AsSpan().Fill(0xFF);

        var serialized = Asn1Serializer.Serialize(new OctetString(content));

        var header = H("04 " + expectedLength);
        Assert.Equal(Convert.ToHexString(header), Convert.ToHexString(serialized.AsSpan(0, header.Length)));
        Assert.Equal(header.Length + length, serialized.Length);
        var parsed = Asn1Serializer.Deserialize(serialized);
        Assert.Equal(length, parsed.Content.Length);
        Assert.True(content.AsSpan().SequenceEqual(parsed.Content));
    }

    [Fact]
    public void ContainerLength_CountsAllNestedBytes()
    {
        var children = Enumerable.Range(0, 70000 / 3).Select(i => (ITag)new Integer([(byte)(i % 100)])).ToList();
        var tag = new Sequence(children);

        var serialized = Asn1Serializer.Serialize(tag);

        Assert.Equal(H("30 83 01 11 6F"), serialized.AsSpan(0, 5).ToArray()); // 23333 × 3 bytes = 69999
        TreeAssert.Equal(tag, Asn1Serializer.Deserialize(serialized));
    }

    [Fact]
    public void IndefiniteInput_IsWrittenWithDefiniteLengths()
    {
        var parsed = Asn1Serializer.Deserialize(H("30 80 02 01 05 A0 80 05 00 00 00 00 00"));

        Assert.Equal(H("30 07 02 01 05 A0 02 05 00"), Asn1Serializer.Serialize(parsed));
    }
}
