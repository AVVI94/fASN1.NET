using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;
using static fASN1.NET.Tests.Infrastructure.Expected;

namespace fASN1.NET.Tests;

/// <summary>
/// Characterization tests for parser behavior that is known to be wrong or surprising.
/// They pin the current behavior so the iterative rewrite cannot change it by accident.
/// When one of these is fixed, move the test to the corrected behavior.
/// </summary>
public class KnownQuirkTests
{
    private static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    private static ITag Parse(string hex)
    {
        var data = H(hex);
        Contract.Check(data);
        Assert.True(Asn1Serializer.TryDeserialize(data, out var tag, out var error), error);
        return tag;
    }

    [Fact]
    public void EmptyIndefiniteContainer_ReadsFollowingBytesAsContent()
    {
        // GetChildren returns the consumed length (the EOC itself) through ref,
        // and the empty container then reads that many bytes after the EOC.
        var tag = Parse("30 80 00 00 02 01 07");

        Assert.IsType<Sequence>(tag);
        Assert.Empty(tag.Children);
        Assert.Equal(H("0201"), tag.Content);
    }

    [Fact]
    public void InvalidChildInsideIndefiniteContainer_IsDroppedSilently()
    {
        // The second INTEGER claims 5 bytes; the indefinite loop treats the failure like EOC.
        var tag = Parse("30 80 02 01 05 02 05 01 00 00");

        Seq(Int("05")).AssertMatches(tag);
    }

    [Fact]
    public void FiveByteLength_OverflowsToZero()
    {
        Octets("").AssertMatches(Parse("04 85 01 00 00 00 00"));
    }

    [Fact]
    public void ApplicationPrivateUnknown_TagNumberIsZero()
    {
        Assert.Equal(0, Parse("41 01 07").TagNumber);
        Assert.Equal(0, Parse("C1 01 07").TagNumber);
        Assert.Equal(0, Parse("0D 01 07").TagNumber);
        Assert.Equal(H("000107"), Asn1Serializer.Serialize(Parse("41 01 07")));
    }

    [Fact]
    public void UnknownUniversalTag_IsReportedAsEoc()
    {
        var tag = Parse("24 03 04 01 41");

        Assert.IsType<Unknown>(tag);
        Assert.True(tag.IsEoc);
        Assert.Single(tag.Children);
    }

    [Fact]
    public void UnknownUniversalTag_EndsEnclosingIndefiniteContainer()
    {
        var data = H("30 80 24 80 04 01 41 00 00 02 01 05 00 00");
        Contract.Check(data);

        Assert.False(Asn1Serializer.TryDeserialize(data, out _, out var error));
        Assert.Equal("Content size is not correct for tag Sequence at offset 2", error);
    }

    [Fact]
    public void EmbeddedPdv_TagNumberIsTenAndAlwaysConstructed()
    {
        Assert.Equal(10, new EmbeddedPdv().TagNumber);
        new Expected(typeof(EmbeddedPdv), 10, "EmbeddedPDV", 0, true, true, false, [], [Int("05")]).AssertMatches(Parse("0B 03 02 01 05"));

        var primitive = H("0B 02 41 42");
        Contract.Check(primitive);
        Assert.False(Asn1Serializer.TryDeserialize(primitive, out _, out _));
    }

    [Theory]
    [InlineData("02")]                // EOF where the length should be
    [InlineData("30 82")]             // EOF inside a long-form length
    [InlineData("02 87 00 00 00 00 00 00 01 05")] // more than 6 length octets
    public void SomeFailures_HaveNoErrorMessage(string hex)
    {
        var data = H(hex);
        Contract.Check(data);

        Assert.False(Asn1Serializer.TryDeserialize(data, out var tag, out var error));
        Assert.Null(tag);
        Assert.Null(error); // violates [NotNullWhen(false)] on the error parameter

        var ex = Assert.Throws<Asn1DeserializationException>(() => Asn1Serializer.Deserialize(data));
        Assert.Equal("Unknown deserialization error", ex.Message);
    }

    [Fact]
    public void HighTagNumberForm_IsNotSupported()
    {
        var data = H("1F 21 01 05");
        Contract.Check(data);

        Assert.False(Asn1Serializer.TryDeserialize(data, out _, out var error));
        Assert.Equal("Content size is not correct for tag Unknown_31 at offset 2", error);
    }

    [Fact]
    public void IndefiniteInput_ReserializesAsDefinite()
    {
        Assert.Equal(H("30 03 02 01 05"), Asn1Serializer.Serialize(Parse("30 80 02 01 05 00 00")));
    }
}
