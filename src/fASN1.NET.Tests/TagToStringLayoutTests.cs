using System.Text;
using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// How TagToString lays out values: every line of the text belongs to the tree, also the lines of a value that has
/// line breaks of its own (an XML document in an OCTET STRING, for example).
/// </summary>
public class TagToStringLayoutTests
{
    private static readonly string NL = Environment.NewLine;

    [Theory]
    [InlineData("line one\nline two")]
    [InlineData("line one\r\nline two")]
    [InlineData("line one\rline two")]
    [InlineData("line one\nline two\n")] // a final line break adds no empty line
    public void ValueWithLineBreaks_EveryLineIsIndentedUnderTheTag(string value)
    {
        var text = Asn1Serializer.TagToString(InSequence(value));

        Assert.Equal($"Sequence{NL} | OctetString{NL} |  | line one{NL} |  | line two", text);
    }

    [Fact]
    public void ValueWithLineBreaks_LongLinesAreWrappedOnTheirOwn()
    {
        var text = Asn1Serializer.TagToString(InSequence("short\n" + new string('x', 25)), o => o.MaximumContentLineLength = 10);

        Assert.Equal(
            $"Sequence{NL} | OctetString{NL} |  | short{NL} |  | xxxxxxxxxx{NL} |  | xxxxxxxxxx{NL} |  | xxxxx",
            text);
    }

    [Fact]
    public void ValueWithLineBreaks_WordWrapStaysWithinEachLine()
    {
        var text = Asn1Serializer.TagToString(InSequence("first line\nthe second line is longer"), o =>
        {
            o.MaximumContentLineLength = 16;
            o.ContentLengthHandling = ContentLengthHandling.WordWrap;
        });

        Assert.Equal(
            $"Sequence{NL} | OctetString{NL} |  | first line{NL} |  | the second{NL} |  | line is{NL} |  | longer",
            text);
    }

    [Fact]
    public void ValueWithLineBreaks_TruncatedValueKeepsItsLinesIndented()
    {
        var text = Asn1Serializer.TagToString(InSequence("line one\nline two is long"), o =>
        {
            o.MaximumContentLineLength = 16;
            o.ContentLengthHandling = ContentLengthHandling.Truncate;
        });

        // The limit counts the whole value, line break included: the first 13 characters and the truncation mark.
        Assert.Equal($"Sequence{NL} | OctetString{NL} |  | line one{NL} |  | line...", text);
    }

    [Fact]
    public void ValueWithLineBreaks_WithoutALineLimitIsStillIndented()
    {
        var text = Asn1Serializer.TagToString(InSequence("line one\nline two"), o => o.MaximumContentLineLength = 0);

        Assert.Equal($"Sequence{NL} | OctetString{NL} |  | line one{NL} |  | line two", text);
    }

    [Fact]
    public void SingleLineValue_StaysNextToTheTagName()
    {
        Assert.Equal($"Sequence{NL} | OctetString short", Asn1Serializer.TagToString(InSequence("short")));
    }

    [Fact]
    public void XmlInACmsSignedData_EveryLineBelongsToTheTree()
    {
        // The renewal request's payload is an XML document of 1815 octets with line breaks.
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get("file:cms_signed_ber_renewal"));

        var lines = Asn1Serializer.TagToString(tag).Split(NL);

        Assert.All(lines.Skip(1), line => Assert.StartsWith(" | ", line));
        Assert.Contains(lines, line => line.EndsWith("<Email>novak@test.cz</Email>"));
        Assert.DoesNotContain(lines, line => line.EndsWith("OctetString "));
    }

    private static ITag InSequence(string value) => new Sequence([new OctetString(Encoding.UTF8.GetBytes(value))]);
}
