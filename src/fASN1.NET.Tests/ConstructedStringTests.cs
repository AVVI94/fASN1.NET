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
    private static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    private static ITag Parse(string hex)
    {
        var (tag, error) = Contract.Check(H(hex));
        Assert.True(tag is not null, error);
        return tag!;
    }

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
}
