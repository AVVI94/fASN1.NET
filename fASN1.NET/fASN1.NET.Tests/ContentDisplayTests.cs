using fASN1.NET.ContentParsing;

namespace fASN1.NET.Tests;

/// <summary>
/// How content without a defined text form (OCTET STRING, non-universal tags) is shown.
/// </summary>
public class ContentDisplayTests
{
    [Theory]
    [InlineData("41686F6A", "Ahoj")]
    [InlineData("50C599C3AD6C69C5A1", "Příliš")]
    [InlineData("610962", "a\tb")]
    [InlineData("", "")]
    public void OctetString_ValidPrintableUtf8_IsText(string hex, string expected)
    {
        Assert.Equal(expected, OctetStringParsingStrategy.Default.Parse(Convert.FromHexString(hex)));
    }

    [Theory]
    [InlineData("C328")]         // invalid UTF-8; used to become "�(", which passed as printable
    [InlineData("FFFE4142")]     // invalid UTF-8 bytes among text
    [InlineData("E282")]         // truncated multi-byte sequence
    [InlineData("410042")]       // NUL
    [InlineData("417F")]         // DEL
    [InlineData("41C28542")]     // valid UTF-8 for U+0085, a C1 control
    public void OctetString_BinaryOrControlCharacters_IsHex(string hex)
    {
        Assert.Equal(hex, OctetStringParsingStrategy.Default.Parse(Convert.FromHexString(hex)));
    }

    [Theory]
    [InlineData("FF", "True")]
    [InlineData("01", "True")]   // any non-zero byte is TRUE in BER; used to show False
    [InlineData("00", "False")]
    [InlineData("", "")]
    [InlineData("0000", "0000")] // not a BOOLEAN; used to show False
    public void Boolean_FollowsBer(string hex, string expected)
    {
        Assert.Equal(expected, BooleanParsingStrategy.Default.Parse(Convert.FromHexString(hex)));
    }

    [Fact]
    public void UniversalString_IsBigEndianUcs4()
    {
        // "A", "ř" (U+0159) and "𝄞" (U+1D11E, outside the BMP), four bytes each, most significant first
        var tag = Asn1Serializer.Deserialize(Convert.FromHexString("1C0C00000041000001590001D11E"));

        Assert.Equal("Ař𝄞", tag.ContentToString());
    }

    [Theory]
    [InlineData("4D756C6C6572", "Muller")]
    [InlineData("4DFC6C6C6572", "Müller")]         // Latin-1
    [InlineData("4DC3BC6C6C6572", "Müller")]       // UTF-8
    public void TeletexString_IsLatin1OrUtf8(string hex, string expected)
    {
        Assert.Equal(expected, Asn1Serializer.Deserialize(Convert.FromHexString($"14{hex.Length / 2:X2}{hex}")).ContentToString());
    }

    [Fact]
    public void KeyUsageCriticalFlag_AnyNonZeroByteIsTrue()
    {
        // SEQUENCE { OID keyUsage, BOOLEAN 0x01, OCTET STRING { BIT STRING digitalSignature } }
        var tag = Asn1Serializer.Deserialize(Convert.FromHexString("300E0603551D0F010101040403020780"));

        Assert.True(tag.TryGetKeyUsage(out var keyUsage));
        Assert.True(keyUsage.Critical);
    }
}
