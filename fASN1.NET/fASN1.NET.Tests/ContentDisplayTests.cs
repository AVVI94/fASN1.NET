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
}
