using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;
using static fASN1.NET.Tests.Infrastructure.Expected;

namespace fASN1.NET.Tests;

/// <summary>
/// Characterization tests for known bugs outside the parser itself: EMBEDDED PDV reporting the wrong
/// TagNumber, and the serializer writing definite lengths only. They pin the current behavior
/// so it does not change by accident. When one of these is fixed, move the test to the corrected behavior.
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
    public void EmbeddedPdv_TagNumberIsTenAndAlwaysConstructed()
    {
        Assert.Equal(10, new EmbeddedPdv().TagNumber);
        new Expected(typeof(EmbeddedPdv), 10, "EmbeddedPDV", 0, true, true, false, [], [Int("05")]).AssertMatches(Parse("0B 03 02 01 05"));

        var primitive = H("0B 02 41 42");
        Contract.Check(primitive);
        Assert.False(Asn1Serializer.TryDeserialize(primitive, out _, out _));
    }

    [Fact]
    public void IndefiniteInput_ReserializesAsDefinite()
    {
        Assert.Equal(H("30 03 02 01 05"), Asn1Serializer.Serialize(Parse("30 80 02 01 05 00 00")));
    }
}
