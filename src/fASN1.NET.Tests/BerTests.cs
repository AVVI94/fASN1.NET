using System.Formats.Asn1;
using System.Numerics;
using System.Text;
using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// BER (X.690 §8) as a whole, by clause. Every valid input is compared node by node with System.Formats.Asn1 reading
/// it as BER, and serializing it must give DER structure; every invalid input must be rejected by both.
/// </summary>
public class BerTests
{
    public static TheoryData<string, string> ValidBer => new()
    {
        // §8.1.2 identifiers
        { "every class, primitive and constructed", "30 1C 41 01 01 82 01 02 C3 01 03 61 03 02 01 04 A2 03 02 01 05 E3 03 02 01 06 1C 00 05 00" },
        { "multi-byte tag numbers in every class", "30 13 1F 1F 01 01 5F 81 00 01 02 BF 28 03 02 01 03 DF 87 68 00" },
        { "multi-byte tag with indefinite length", "BF 28 80 02 01 05 00 00" },
        { "universal number 31 and up is unknown", "30 06 1F 24 00 3F 24 00" },

        // §8.1.3 lengths
        { "short form", "04 03 41 42 43" },
        { "long form for a short value", "04 81 03 41 42 43" },
        { "long form with leading zeros", "04 82 00 03 41 42 43" },
        { "six length octets", "04 86 00 00 00 00 00 03 41 42 43" },
        { "non-minimal lengths at every level", "30 82 00 0A 30 81 07 02 81 01 05 05 81 00" },
        { "long form with 128 bytes", "04 81 80 " + string.Concat(Enumerable.Repeat("FF", 128)) },
        { "indefinite SEQUENCE", "30 80 02 01 05 00 00" },
        { "empty indefinite SEQUENCE", "30 80 00 00" },
        { "nested indefinite", "30 80 30 80 30 80 02 01 05 00 00 00 00 00 00" },
        { "definite inside indefinite", "30 80 30 03 02 01 05 00 00" },
        { "indefinite inside definite", "30 07 30 80 02 01 05 00 00" },
        { "indefinite siblings", "30 80 30 80 00 00 31 80 00 00 A0 80 00 00 00 00" },
        { "indefinite context-specific", "A0 80 02 01 05 00 00" },
        { "indefinite application", "61 80 02 01 05 00 00" },
        { "indefinite private", "E1 80 02 01 05 00 00" },

        // §8.2 BOOLEAN: any non-zero byte is TRUE
        { "BOOLEAN 0x01", "01 01 01" },
        { "BOOLEAN 0x7F", "01 01 7F" },

        // §8.6 BIT STRING
        { "unused bits that are not zero", "03 02 04 F7" },
        { "constructed BIT STRING", "23 08 03 02 00 0A 03 02 04 F0" },
        { "constructed BIT STRING, indefinite", "23 80 03 02 00 0A 03 02 04 F0 00 00" },
        { "constructed BIT STRING, nested fragment", "23 80 23 04 03 02 00 0A 03 02 04 F0 00 00" },
        { "constructed BIT STRING, no fragments", "23 00" },

        // §8.7 OCTET STRING
        { "constructed OCTET STRING", "24 06 04 01 41 04 01 42" },
        { "constructed OCTET STRING, indefinite", "24 80 04 01 41 04 01 42 00 00" },
        { "constructed OCTET STRING, nested indefinite", "24 80 04 02 41 42 24 80 04 01 43 00 00 00 00" },
        { "constructed OCTET STRING, empty fragments", "24 80 04 00 04 00 00 00" },
        { "constructed OCTET STRING, long-form fragment lengths", "24 08 04 81 01 41 04 81 01 42" },
        { "constructed OCTET STRING holding DER", "24 09 04 02 30 03 04 03 02 01 05" },
        { "constructed OCTET STRING inside a SEQUENCE", "30 80 24 80 04 01 41 00 00 02 01 05 00 00" },

        // §8.9 to §8.12 SEQUENCE and SET
        { "SET in any order", "31 80 02 01 09 02 01 01 00 00" },
        { "SET with duplicates", "31 06 02 01 05 02 01 05" },

        // §8.23 restricted character strings
        { "constructed UTF8String split inside a character", "2C 80 04 02 50 C5 04 01 99 00 00" },
        { "constructed IA5String", "36 80 04 01 41 04 01 42 00 00" },
        { "constructed PrintableString", "33 06 04 01 41 04 01 42" },
        { "constructed BMPString", "3E 80 04 02 00 41 04 02 00 42 00 00" },
        { "constructed TeletexString", "34 06 04 01 41 04 01 FC" },

        // §8.25, §8.26 times are VisibleStrings and may be constructed too
        { "constructed UTCTime", "37 80 04 06 32 35 30 31 30 32 04 07 30 33 30 34 30 35 5A 00 00" },
        { "constructed GeneralizedTime", "38 13 04 08 32 30 32 35 30 31 30 32 04 07 30 33 30 34 30 35 5A" },

        // BER inside OCTET STRINGs: read as children only in DER form, otherwise kept as raw bytes
        { "OCTET STRING holding indefinite length", "04 07 30 80 02 01 05 00 00" },
        { "OCTET STRING holding a non-minimal length", "04 06 30 81 03 02 01 05" },
        { "OCTET STRING holding an unsorted SET", "04 08 31 06 02 01 09 02 01 01" },
        { "OCTET STRING holding a constructed string", "04 05 24 03 04 01 41" },

        // everything at once
        { "a mix", "30 80 A0 80 24 80 04 01 41 04 01 42 00 00 31 80 02 01 09 02 01 01 00 00 00 00 BF 28 81 03 02 01 05 01 01 01 00 00" },
    };

    public static TheoryData<string, string, string> InvalidBer => new()
    {
        { "missing end-of-contents", "30 80 02 01 05", "The contents in the indefinite form at offset 2 have no end-of-contents octets" },
        { "missing end-of-contents, nested", "30 80 30 80 02 01 05 00 00", "The contents in the indefinite form at offset 2 have no end-of-contents octets" },
        { "end-of-contents with a length", "30 80 00 01 05 00 00", "Invalid end-of-contents octets at offset 2" },
        { "indefinite primitive", "02 80 01 05 00 00", "The primitive encoding with contents at offset 2 has a length in the indefinite form" },
        { "indefinite primitive OCTET STRING", "04 80 04 01 41 00 00", "The primitive encoding with contents at offset 2 has a length in the indefinite form" },
        { "truncated content", "30 03 02 01", "The contents at offset 2 have a length of 3, which reaches past the end of the data" },
        { "truncated length", "04 82 00", "Unexpected end of data in the length octets of the encoding at offset 0" },
        { "length 0xFF is reserved", "04 FF 00", "The length of the encoding at offset 0 uses 127 octets; at most 6 are supported" },
        { "child crossing its container", "30 02 02 01 05", "The nested encodings do not match the length of the contents at offset 2" },
        { "indefinite child without room for its end", "30 05 30 80 02 01 05", "The contents in the indefinite form at offset 4 have no end-of-contents octets" },
        { "tag number with a leading zero digit", "1F 80 21 01 05", "The tag number of the encoding at offset 0 is not in the shortest form" },
        { "low tag number in the long form", "1F 1E 01 05", "The tag number of the encoding at offset 0 is not in the shortest form" },
        { "truncated identifier", "1F 81", "Unexpected end of data in the identifier octets of the encoding at offset 0" },
        { "fragment of the wrong type", "24 03 02 01 05", "The fragment at offset 2 of a constructed string is not an OCTET STRING" },
        { "character string fragment of its own type", "2C 03 0C 01 41", "The fragment at offset 2 of a constructed string is not an OCTET STRING" },
        { "unused bits before the last fragment", "23 08 03 02 04 F0 03 02 00 0A", "The BIT STRING fragment at offset 6 has an invalid number of unused bits; only the last fragment may have unused bits" },
        { "unused bits above 7", "23 04 03 02 08 00", "The BIT STRING fragment at offset 2 has an invalid number of unused bits; only the last fragment may have unused bits" },
        { "empty BIT STRING fragment", "23 02 03 00", "The BIT STRING fragment at offset 2 has an invalid number of unused bits; only the last fragment may have unused bits" },
        { "constructed string without its end", "24 80 04 01 41", "The contents in the indefinite form at offset 2 have no end-of-contents octets" },
    };

    [Theory]
    [MemberData(nameof(ValidBer))]
    public void ValidBer_MatchesDotNetNodeForNode(string _, string hex)
    {
        var data = H(hex);

        var (tag, error) = Contract.Check(data);

        Assert.True(tag is not null, error);
        OracleNode.Decode(data).AssertMatches(tag!);
    }

    [Theory]
    [MemberData(nameof(ValidBer))]
    public void ValidBer_SerializesWithDerStructure(string _, string hex)
    {
        var tag = Asn1Serializer.Deserialize(H(hex));

        AssertDerStructure(Asn1Serializer.Serialize(tag));
    }

    [Theory]
    [MemberData(nameof(InvalidBer))]
    public void InvalidBer_IsRejectedLikeDotNetRejectsIt(string _, string hex, string expectedError)
    {
        var data = H(hex);

        var (tag, error) = Contract.Check(data);

        Assert.Null(tag);
        Assert.Equal(expectedError, error);
        Assert.ThrowsAny<Exception>(() => OracleNode.Decode(data));
    }

    [Fact]
    public void CerFromAsnWriter_ReadsBackEveryValue_AndSerializesToTheDerOfTheSameValues()
    {
        // CER writes constructed values with indefinite lengths and splits strings of more than 1000 bytes into
        // 1000-byte fragments; DER writes the same values with definite lengths and as single strings.
        var octets = Enumerable.Range(0, 2500).Select(i => (byte)i).ToArray();
        var text = string.Concat(Enumerable.Repeat("Příliš žluťoučký kůň ", 80));
        var bits = Enumerable.Range(0, 1200).Select(i => (byte)(i * 7)).ToArray();
        bits[^1] &= 0xF8; // three unused bits, zero as CER and DER require
        var time = new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.Zero);

        byte[] Write(AsnEncodingRules rules)
        {
            var writer = new AsnWriter(rules);
            using (writer.PushSequence())
            {
                writer.WriteOctetString(octets);
                writer.WriteCharacterString(UniversalTagNumber.UTF8String, text);
                writer.WriteBitString(bits, unusedBitCount: 3);
                using (writer.PushSetOf())
                {
                    foreach (var value in new[] { 300, 2, -5, 70000 })
                    {
                        writer.WriteInteger(value);
                    }
                }
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 40)))
                {
                    writer.WriteInteger(BigInteger.Parse("123456789012345678901234567890"));
                    writer.WriteGeneralizedTime(time);
                    writer.WriteBoolean(true);
                }
            }
            return writer.Encode();
        }
        var cer = Write(AsnEncodingRules.CER);
        var der = Write(AsnEncodingRules.DER);
        Assert.Contains((byte)0x80, cer[..2]); // really indefinite at the top

        var (tag, error) = Contract.Check(cer);
        Assert.True(tag is not null, error);
        OracleNode.Decode(cer).AssertMatches(tag!);

        Assert.Equal(octets, Assert.IsType<OctetString>(tag!.Children[0]).Content);
        Assert.Equal(text, Assert.IsType<Utf8String>(tag.Children[1]).ContentToString());
        Assert.Equal([3, .. bits], Assert.IsType<BitString>(tag.Children[2]).Content);
        // SET OF in CER and DER is sorted by encoding: 02 01 02 (2) before 02 01 FB (-5).
        Assert.Equal(["2", "-5", "300", "70000"], tag.Children[3].Children.Select(c => c.ContentToString()));
        var tagged = Assert.IsType<ContextSpecificTag>(tag.Children[4]);
        Assert.Equal(40, tagged.TagNumber);
        Assert.Equal("123456789012345678901234567890", tagged.Children[0].ContentToString());
        Assert.Equal("2031-02-03T04:05:06Z", tagged.Children[1].ContentToString());
        Assert.Equal("True", tagged.Children[2].ContentToString());

        Assert.Equal(Convert.ToHexString(der), Convert.ToHexString(Asn1Serializer.Serialize(tag)));
    }

    [Fact]
    public void BerFromAStream_ReadsTheSameAsFromAnArray()
    {
        var data = H("30 80 24 80 04 01 41 04 01 42 00 00 BF 28 80 02 01 05 00 00 00 00");
        using var stream = new MemoryStream([0xEE, .. data, 0xEE]) { Position = 1 };

        var fromStream = Asn1Serializer.Deserialize(stream);

        TreeAssert.Equal(Asn1Serializer.Deserialize(data), fromStream);
        Assert.Equal(1 + data.Length, stream.Position); // left just after the element
    }

    private static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    /// <summary>
    /// Checks what DER requires of the structure: definite, minimal lengths, no constructed strings, and SET
    /// components in ascending order. Value rules (BOOLEAN as 00/FF, zero padding bits) are not structure: the
    /// serializer keeps values as they are.
    /// </summary>
    private static void AssertDerStructure(byte[] encoding)
    {
        var pending = new Stack<ReadOnlyMemory<byte>>();
        AsnDecoder.ReadEncodedValue(encoding, AsnEncodingRules.DER, out _, out _, out var consumed);
        Assert.Equal(encoding.Length, consumed);
        pending.Push(encoding);
        while (pending.Count > 0)
        {
            var element = pending.Pop();
            var tag = Asn1Tag.Decode(element.Span, out _);
            AsnDecoder.ReadEncodedValue(element.Span, AsnEncodingRules.DER, out var offset, out var length, out _);
            if (!tag.IsConstructed)
            {
                continue;
            }

            Assert.False(tag.TagClass == TagClass.Universal && tag.TagValue is 3 or 4 or 7 or 12 or (>= 18 and <= 28) or 30,
                $"constructed string {tag} in DER output");
            if (tag.TagClass == TagClass.Universal && tag.TagValue == 17)
            {
                new AsnReader(element, AsnEncodingRules.DER).ReadSetOf(); // throws when out of order
            }

            var content = element.Slice(offset, length);
            while (!content.IsEmpty)
            {
                AsnDecoder.ReadEncodedValue(content.Span, AsnEncodingRules.DER, out _, out _, out var childLength);
                pending.Push(content[..childLength]);
                content = content[childLength..];
            }
        }
    }
}
