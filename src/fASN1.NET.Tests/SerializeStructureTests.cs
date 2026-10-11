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

    /// <summary>
    /// An OCTET STRING value is data, often signed or hashed, so Serialize must write it back unchanged. Content
    /// that is valid BER but not in DER length form used to be read as children and re-encoded with DER lengths,
    /// which changed the value. Such content now stays a leaf.
    /// </summary>
    [Theory]
    [InlineData("30 81 03 02 01 05")]          // long form for a length under 128
    [InlineData("30 82 00 03 02 01 05")]       // leading zero length octet
    [InlineData("30 80 02 01 05 00 00")]       // indefinite length
    [InlineData("30 05 30 81 02 05 00")]       // non-DER length one level down
    [InlineData("04 04 30 81 00 01 02 01 05")] // ... inside a nested OCTET STRING: the inner one stays raw instead
    public void OctetStringWithNonDerContent_SerializesToTheSameBytes(string contentHex)
    {
        var content = H(contentHex);
        byte[] data = [0x04, (byte)content.Length, .. content];

        var parsed = Asn1Serializer.Deserialize(data);

        Assert.Equal(Convert.ToHexString(data), Convert.ToHexString(Asn1Serializer.Serialize(parsed)));
    }

    [Fact]
    public void OctetStringWithNonDerContent_IsALeafHoldingTheRawBytes()
    {
        var parsed = Asn1Serializer.Deserialize(H("04 06 30 81 03 02 01 05"));

        Assert.Empty(parsed.Children);
        Assert.Equal(H("30 81 03 02 01 05"), parsed.Content);
    }

    [Fact]
    public void OctetStringWithDerContent_IsStillReadAsChildren()
    {
        var parsed = Asn1Serializer.Deserialize(H("04 05 30 03 02 01 05"));

        var sequence = Assert.IsType<Sequence>(Assert.Single(parsed.Children));
        Assert.Equal(H("05"), Assert.Single(sequence.Children).Content);
    }

    [Fact]
    public void BerOutsideOctetStrings_IsStillAccepted()
    {
        // Lengths of the structure itself are not values; Serialize normalizes them to DER as before.
        var parsed = Asn1Serializer.Deserialize(H("30 81 03 02 01 05"));

        Assert.Equal(H("30 03 02 01 05"), Asn1Serializer.Serialize(parsed));
    }

    [Fact]
    public void BitStringWithChildren_GetsTheUnusedBitsByte()
    {
        // A public key is a BIT STRING holding a DER structure; the leading 0x00 used to be left out.
        var key = new Sequence([new Integer(5)]);

        var serialized = Asn1Serializer.Serialize(new BitString(children: [key]));

        var writer = new System.Formats.Asn1.AsnWriter(System.Formats.Asn1.AsnEncodingRules.DER);
        writer.WriteBitString(Asn1Serializer.Serialize(key));
        Assert.Equal(Convert.ToHexString(writer.Encode()), Convert.ToHexString(serialized));
    }

    [Fact]
    public void OctetStringWithChildren_StaysPrimitive()
    {
        Assert.Equal(H("04 05 30 03 02 01 05"), Asn1Serializer.Serialize(new OctetString(children: [new Sequence([new Integer(5)])])));
    }

    [Fact]
    public void ElementWithChildren_GetsTheConstructedBit()
    {
        // [0] created as primitive but holding children, as in an EXPLICIT tag built by hand.
        var tag = new ContextSpecific_0(isConstructed: false, children: [new Integer(5)]);

        Assert.Equal(H("A0 03 02 01 05"), Asn1Serializer.Serialize(tag));
        Assert.Equal(H("30 03 02 01 05"), Asn1Serializer.Serialize(new UniversalTag(0x10, children: [new Integer(5)])));
    }

    [Theory]
    [InlineData(30, "1E")]
    [InlineData(31, "1F 1F")]            // the first number that needs the multi-byte form
    [InlineData(127, "1F 7F")]
    [InlineData(128, "1F 81 00")]
    [InlineData(0x100, "1F 82 00")]      // used to be truncated to 0x00, an end-of-contents marker
    [InlineData(int.MaxValue, "1F 87 FF FF FF 7F")]
    public void TagNumber_IsWrittenInTheShortestForm(int number, string identifier)
    {
        var serialized = Asn1Serializer.Serialize(new UniversalTag(number, [0xAB]));

        Assert.Equal(Convert.ToHexString(H(identifier + " 01 AB")), Convert.ToHexString(serialized));
    }

    [Fact]
    public void NegativeTagNumber_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => Asn1Serializer.Serialize(new Sequence([new UniversalTag(-1, [0x01])])));
    }

    [Fact]
    public void ElementWithContentAndChildren_IsRejected()
    {
        // The content used to be dropped without a word.
        var tag = new UTCTime(H("31"), [new Null()]);

        Assert.Throws<ArgumentException>(() => Asn1Serializer.Serialize(tag));
    }

    [Fact]
    public void Set_IsWrittenInDerOrder()
    {
        var parsed = Asn1Serializer.Deserialize(H("31 06 02 01 09 02 01 01"));

        Assert.Equal(H("31 06 02 01 01 02 01 09"), Asn1Serializer.Serialize(parsed));
    }

    [Fact]
    public void Set_MatchesDotNetSetOfOrder()
    {
        // Mixed tags and lengths, a nested SET out of order, and duplicates. AsnWriter sorts SET OF on encode.
        ITag[] items =
        [
            new Utf8String("zz"u8.ToArray()),
            new Integer(300),
            new Set([new Integer(2), new Integer(1)]),
            new Integer(5),
            new OctetString([0x01]),
            new Integer(5),
            new Null(),
        ];
        var writer = new System.Formats.Asn1.AsnWriter(System.Formats.Asn1.AsnEncodingRules.DER);
        using (writer.PushSetOf())
        {
            foreach (var item in items)
            {
                writer.WriteEncodedValue(Asn1Serializer.Serialize(item));
            }
        }

        var serialized = Asn1Serializer.Serialize(new Set([.. items]));

        Assert.Equal(Convert.ToHexString(writer.Encode()), Convert.ToHexString(serialized));
        Assert.Contains("31060201010201 02".Replace(" ", ""), Convert.ToHexString(serialized)); // inner SET sorted too
    }

    [Fact]
    public void OctetStringWithUnsortedSet_StaysRawAndKeepsItsBytes()
    {
        // Reading it as children would let Serialize reorder the SET and change the value.
        var data = H("04 08 31 06 02 01 09 02 01 01");

        var parsed = Asn1Serializer.Deserialize(data);

        Assert.Empty(parsed.Children);
        Assert.Equal(data, Asn1Serializer.Serialize(parsed));
    }

    [Fact]
    public void OctetStringWithSortedSet_IsStillReadAsChildren()
    {
        var parsed = Asn1Serializer.Deserialize(H("04 08 31 06 02 01 01 02 01 09"));

        Assert.Equal(2, Assert.IsType<Set>(Assert.Single(parsed.Children)).Children.Count);
    }
}
