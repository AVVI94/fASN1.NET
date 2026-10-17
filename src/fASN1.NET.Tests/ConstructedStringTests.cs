using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;
using static fASN1.NET.Tests.Infrastructure.Expected;

namespace fASN1.NET.Tests;

/// <summary>
/// BER constructed strings (X.690 §8.6.4, §8.7.3, §8.23.6): the fragments are joined into one value, so the tree
/// looks as it would for the primitive encoding of the same value.
/// </summary>
public class ConstructedStringTests
{
    [Theory]
    [InlineData("24 06 04 01 41 04 01 42")]                // definite length
    [InlineData("24 80 04 01 41 04 01 42 00 00")]          // indefinite length
    [InlineData("24 80 24 03 04 01 41 04 01 42 00 00")]    // a constructed fragment
    [InlineData("24 08 04 00 04 02 41 42 04 00")]          // empty fragments
    public void OctetString_FragmentsAreJoined(string hex)
    {
        Octets("4142").AssertMatches(Parse(hex));
    }

    [Fact]
    public void OctetString_JoinedValue_IsReadAsEncapsulatedElementsLikeAPrimitiveOne()
    {
        // The two fragments hold 02 01 05 between them.
        var tag = Parse("24 07 04 02 02 01 04 01 05");

        OctetsWith(Int("05")).AssertMatches(tag);
    }

    [Fact]
    public void CharacterString_FragmentsAreJoined()
    {
        // UTF8String "Příliš" split inside the two-byte "ř"; fragments of character strings are OCTET STRINGs.
        var tag = Parse("2C 0D 04 02 50 C5 04 07 99 C3 AD 6C 69 C5 A1");

        Assert.IsType<Utf8String>(tag);
        Assert.False(tag.IsConstructed);
        Assert.Equal("Příliš", tag.ContentToString());
    }

    [Fact]
    public void BitString_FragmentsAreJoinedAndTheLastUnusedBitsKept()
    {
        // 0A (8 bits) then F0 with 4 unused bits; the joined value is 04 0A F0.
        var tag = Parse("23 08 03 02 00 0A 03 02 04 F0");

        Bits("040AF0").AssertMatches(tag);
    }

    [Fact]
    public void ConstructedString_SerializesAsThePrimitiveDerForm()
    {
        Assert.Equal(H("04 02 41 42"), Asn1Serializer.Serialize(Parse("24 80 04 01 41 04 01 42 00 00")));
        Assert.Equal(H("03 03 04 0A F0"), Asn1Serializer.Serialize(Parse("23 08 03 02 00 0A 03 02 04 F0")));
    }

    [Theory]
    [InlineData("24 03 02 01 05", "The fragment at offset 2 of a constructed string is not an OCTET STRING")]
    [InlineData("2C 03 0C 01 41", "The fragment at offset 2 of a constructed string is not an OCTET STRING")]
    [InlineData("23 03 04 01 00", "The fragment at offset 2 of a constructed string is not a BIT STRING")]
    [InlineData("23 08 03 02 04 F0 03 02 00 0A", "The BIT STRING fragment at offset 6 has an invalid number of unused bits; only the last fragment may have unused bits")]
    [InlineData("23 03 03 01 01", "The BIT STRING fragment at offset 2 has an invalid number of unused bits; only the last fragment may have unused bits")]
    [InlineData("23 04 03 02 08 00", "The BIT STRING fragment at offset 2 has an invalid number of unused bits; only the last fragment may have unused bits")]
    [InlineData("24 80 04 01 41", "The contents in the indefinite form at offset 2 have no end-of-contents octets")]
    public void InvalidFragments_Fail(string hex, string error)
    {
        var (tag, actual) = Contract.Check(H(hex));

        Assert.Null(tag);
        Assert.Equal(error, actual);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(100)]
    [InlineData(5_000)]
    public void OctetString_DeeplyNestedFragments_AreJoined(int depth)
    {
        // Each level is an indefinite-length constructed OCTET STRING with a fragment of its own before the next level.
        var data = new List<byte>();
        for (int i = 0; i < depth; i++)
        {
            data.AddRange([0x24, 0x80, 0x04, 0x01, 0x41]);
        }

        data.AddRange([0x04, 0x01, 0x42]);
        for (int i = 0; i < depth; i++)
        {
            data.AddRange([0x00, 0x00]);
        }

        var tag = Asn1Serializer.Deserialize(data.ToArray());

        Assert.IsType<OctetString>(tag);
        Assert.Empty(tag.Children);
        Assert.Equal([.. Enumerable.Repeat((byte)0x41, depth), 0x42], tag.Content);
    }

    [Fact]
    public void OctetString_MixedNestedFragments_AreJoinedInOrder()
    {
        // Indefinite and definite constructed fragments around primitive ones: 41, 42 43, 44, 45.
        Octets("4142434445").AssertMatches(Parse("24 80 24 80 04 01 41 24 04 04 02 42 43 00 00 04 01 44 24 03 04 01 45 00 00"));
    }

    [Theory]
    [InlineData("24 80 24 80 04 01 41 00 00", 2)]          // the outer level has none
    [InlineData("24 80 24 80 04 01 41", 4)]                // neither level has one: the inner one is reported
    [InlineData("24 80 24 80 24 80 04 01 41 00 00 00 00", 2)] // only the outermost has none
    [InlineData("23 80 23 80 03 02 00 41 00 00", 2)]
    public void ConstructedString_MissingEndOfContentsAtAnyLevel_Fails(string hex, int contentsOffset)
    {
        var (tag, error) = Contract.Check(H(hex));

        Assert.Null(tag);
        Assert.Equal($"The contents in the indefinite form at offset {contentsOffset} have no end-of-contents octets", error);
    }

    [Fact]
    public void ConstructedString_InnerIndefiniteFragmentCrossingADefiniteOuter_Fails()
    {
        // The inner fragment's end-of-contents octets lie after the end of the outer contents, so it has none.
        var (tag, error) = Contract.Check(H("24 05 24 80 04 01 41 00 00"));

        Assert.Null(tag);
        Assert.Equal("The contents in the indefinite form at offset 4 have no end-of-contents octets", error);
    }

    [Fact]
    public void ConstructedStringInsideAnOctetString_KeepsTheRawBytes()
    {
        // DER has no constructed strings, and joining would change the bytes Serialize writes.
        var data = H("04 05 24 03 04 01 41");

        var tag = Parse("04 05 24 03 04 01 41");

        Octets("2403040141").AssertMatches(tag);
        Assert.Equal(data, Asn1Serializer.Serialize(tag));
    }

    [Fact]
    public void CmsSample_LongContentInFragmentsIsOneValue()
    {
        // The console tester's CMS sample carries an XML document as a constructed OCTET STRING of 1000-byte
        // fragments; it used to come back as fragments under an Unknown tag with no content.
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get("file:cms_signed_ber_indefinite"));

        var xml = TreeAssert.Walk(tag).Select(n => n.Tag).OfType<OctetString>()
                            .Single(o => o.Content.AsSpan().StartsWith("<?xml"u8));
        Assert.True(xml.Content.Length > 1000);
        Assert.Empty(xml.Children);
        Assert.DoesNotContain(TreeAssert.Walk(tag), n => n.Tag is Unknown);
    }

    private static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    private static ITag Parse(string hex)
    {
        var (tag, error) = Contract.Check(H(hex));
        Assert.True(tag is not null, error);
        return tag!;
    }
}
