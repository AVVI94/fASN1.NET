using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;
using static fASN1.NET.Tests.Infrastructure.Expected;

namespace fASN1.NET.Tests;

/// <summary>
/// Hand-written inputs with the complete expected tree written out node by node.
/// Each assertion checks type, TagNumber, TagName, TagClass, IsConstructed, IsUniversal, IsEoc,
/// Content bytes and the ordered children, so no tag can be dropped, added, reordered or
/// mis-typed without failing. Every input is also run through the reference implementation.
/// </summary>
public class ParsedStructureTests
{
    private static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    private static ITag Parse(string hex) => Parse(H(hex));

    private static ITag Parse(byte[] data)
    {
        Differential.AssertSameAsReference(data);
        Assert.True(Asn1Serializer.TryDeserialize(data, out var tag, out var error), error);
        Assert.Null(error);
        TreeAssert.WellFormed(tag);
        return tag;
    }

    private static void AssertTree(string hex, Expected expected) => expected.AssertMatches(Parse(hex));

    private static void AssertFails(string hex, string? expectedError)
    {
        var data = H(hex);
        Differential.AssertSameAsReference(data);
        Assert.False(Asn1Serializer.TryDeserialize(data, out var tag, out var error));
        Assert.Null(tag);
        Assert.Equal(expectedError, error);
    }

    #region Tag identity

    public static IEnumerable<object[]> PrimitiveUniversalIdentifiers =>
        TagIdentity.KnownUniversalIdentifiers.Where(id => id is not (0x30 or 0x31 or 0x0B)).Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(PrimitiveUniversalIdentifiers))]
    public void KnownUniversalPrimitive_KeepsIdentityAndContent(int identifier)
    {
        // 41 42 is not a complete TLV, so OCTET STRING and BIT STRING stay leaves too.
        var tag = Parse([(byte)identifier, 0x02, 0x41, 0x42]);

        new Expected(TagIdentity.ExpectedType(identifier), TagIdentity.ExpectedTagNumber(identifier), TagIdentity.ExpectedName(identifier),
                     0, false, true, identifier == 0x00, H("4142"), []).AssertMatches(tag);
    }

    [Theory]
    [InlineData(0x0D)]
    [InlineData(0x0E)]
    [InlineData(0x0F)]
    [InlineData(0x10)] // primitive SEQUENCE is not valid; it is not mapped to Sequence
    [InlineData(0x11)]
    [InlineData(0x1D)] // CharacterString has no class
    public void UnassignedUniversal_IsUnknownLeaf(int identifier)
    {
        var tag = Parse([(byte)identifier, 0x01, 0x7F]);

        // TagNumber 0 and IsEoc true are current behavior, see KnownQuirkTests.
        new Expected(typeof(Unknown), 0, $"Unknown_{identifier & 0x1F}", 0, false, true, true, [0x7F], []).AssertMatches(tag);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(9)]
    [InlineData(30)]
    public void ContextSpecificPrimitive_KeepsNumberAndContent(int number)
    {
        AssertTree($"{0x80 | number:X2} 02 CA FE", Ctx(0x80 | number, "CAFE"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(30)]
    public void ContextSpecificConstructed_HoldsChildren(int number)
    {
        AssertTree($"{0xA0 | number:X2} 06 02 01 05 01 01 FF",
            CtxWith(0xA0 | number, Int("05"), Leaf<BooleanTag>(1, "Boolean", "FF")));
    }

    [Fact]
    public void Application_PrimitiveAndConstructed()
    {
        // TagNumber 0 is current behavior, see KnownQuirkTests.
        AssertTree("41 01 07", new Expected(typeof(Application), 0, "Application_1", 1, false, false, false, [0x07], []));
        AssertTree("63 03 02 01 05", new Expected(typeof(Application), 0, "Application_3", 1, true, false, false, [], [Int("05")]));
    }

    [Fact]
    public void Private_PrimitiveAndConstructed()
    {
        AssertTree("C2 01 07", new Expected(typeof(Private), 0, "Private_2", 3, false, false, false, [0x07], []));
        AssertTree("E4 03 02 01 05", new Expected(typeof(Private), 0, "Private_4", 3, true, false, false, [], [Int("05")]));
    }

    #endregion

    #region Lengths

    [Fact]
    public void ZeroLength_Nodes()
    {
        AssertTree("05 00", Null());
        AssertTree("04 00", Octets(""));
        AssertTree("03 00", Bits(""));
        AssertTree("30 00", new Expected(typeof(Sequence), 48, "Sequence", 0, true, true, false, [], []));
        AssertTree("31 00", new Expected(typeof(Tags.Set), 49, "Set", 0, true, true, false, [], []));
        AssertTree("A0 00", Ctx(0xA0, ""));
        AssertTree("30 02 05 00", Seq(Null()));
    }

    [Theory]
    [InlineData("02 81 03 01 02 03")]
    [InlineData("02 82 00 03 01 02 03")]
    [InlineData("02 83 00 00 03 01 02 03")]
    [InlineData("02 84 00 00 00 03 01 02 03")]
    [InlineData("02 85 00 00 00 00 03 01 02 03")]
    [InlineData("02 86 00 00 00 00 00 03 01 02 03")]
    public void LongFormLength_IncludingNonMinimal(string hex)
    {
        AssertTree(hex, Int("010203"));
    }

    [Theory]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(65535)]
    [InlineData(65536)]
    [InlineData(70000)]
    public void LongContent_IsReadCompletely(int size)
    {
        // FF FF starts with an unsupported length form, so the encapsulation probe fails at once.
        var content = Enumerable.Range(0, size).Select(i => (byte)(i < 2 ? 0xFF : i * 7)).ToArray();
        var data = new List<byte> { 0x04 };
        data.AddRange(EncodeLength(size));
        data.AddRange(content);

        var tag = Parse(data.ToArray());

        Assert.IsType<OctetString>(tag);
        Assert.Empty(tag.Children);
        Assert.Equal(content, tag.Content);
    }

    internal static byte[] EncodeLength(int length)
    {
        if (length < 0x80)
            return [(byte)length];
        var bytes = BitConverter.GetBytes(length).Reverse().SkipWhile(b => b == 0).ToArray();
        return [(byte)(0x80 | bytes.Length), .. bytes];
    }

    #endregion

    #region Definite-length nesting

    [Fact]
    public void DeepDefiniteNesting_KeepsEveryLevel()
    {
        AssertTree("30 0D 30 0B 30 09 30 07 31 05 02 03 01 02 03",
            Seq(Seq(Seq(Seq(Set(Int("010203")))))));
    }

    [Fact]
    public void Siblings_AreAllPresentInOrder()
    {
        AssertTree("30 11 02 01 01 30 04 02 02 00 80 04 01 41 05 00 0C 01 7A",
            Seq(Int("01"), Seq(Int("0080")), Octets("41"), Null(), Utf8("7A")));
    }

    [Fact]
    public void ThousandSiblings_NoneDropped()
    {
        var body = Enumerable.Range(0, 1000).SelectMany(i => new byte[] { 0x02, 0x02, (byte)(i >> 8), (byte)i }).ToArray();
        byte[] data = [0x30, .. EncodeLength(body.Length), .. body];

        var tag = Parse(data);

        Seq(Enumerable.Range(0, 1000).Select(i => Int($"{i >> 8:X2}{i & 0xFF:X2}")).ToArray()).AssertMatches(tag);
    }

    [Fact]
    public void EocInsideDefiniteContainer_IsAnOrdinaryChild()
    {
        AssertTree("30 05 00 00 02 01 05", Seq(Eoc(), Int("05")));
    }

    #endregion

    #region Indefinite length

    [Fact]
    public void Indefinite_Sequence()
    {
        AssertTree("30 80 02 01 05 04 02 41 42 00 00", Seq(Int("05"), Octets("4142")));
    }

    [Fact]
    public void Indefinite_NestedInIndefinite()
    {
        AssertTree("30 80 30 80 02 01 01 00 00 02 01 02 00 00", Seq(Seq(Int("01")), Int("02")));
    }

    [Fact]
    public void Indefinite_InsideDefiniteParent_FollowedBySibling()
    {
        AssertTree("30 0A 30 80 02 01 01 00 00 02 01 02", Seq(Seq(Int("01")), Int("02")));
    }

    [Fact]
    public void Indefinite_ContextSpecific()
    {
        AssertTree("A0 80 02 01 07 00 00", CtxWith(0xA0, Int("07")));
    }

    [Fact]
    public void Indefinite_DefiniteChildren_MixedDepths()
    {
        AssertTree("30 80 31 06 02 01 01 02 01 02 A1 80 30 03 02 01 03 00 00 05 00 00 00",
            Seq(Set(Int("01"), Int("02")), CtxWith(0xA1, Seq(Int("03"))), Null()));
    }

    [Fact]
    public void Indefinite_PrimitiveTag_Fails()
    {
        AssertFails("02 80 01 00 00", "Cannot skip over an invalid tag with indefinite length");
    }

    [Fact]
    public void Indefinite_OctetString_IsProbedUntilEoc()
    {
        AssertTree("04 80 02 01 05 00 00", OctetsWith(Int("05")));
    }

    #endregion

    #region OCTET STRING encapsulation probe

    [Fact]
    public void Probe_SingleChild() => AssertTree("04 03 02 01 05", OctetsWith(Int("05")));

    [Fact]
    public void Probe_MultipleChildren() =>
        AssertTree("04 06 02 01 05 01 01 FF", OctetsWith(Int("05"), Leaf<BooleanTag>(1, "Boolean", "FF")));

    [Fact]
    public void Probe_Nested() =>
        AssertTree("04 07 04 05 30 03 02 01 05", OctetsWith(OctetsWith(Seq(Int("05")))));

    [Theory]
    [InlineData("04 04 02 01 05 FF", "020105FF")]     // trailing byte is not a complete TLV
    [InlineData("04 02 00 00", "0000")]               // EOC cannot be content
    [InlineData("04 05 02 01 05 00 00", "0201050000")] // EOC as a later child
    [InlineData("04 05 68 65 6C 6C 6F", "68656C6C6F")] // text
    [InlineData("04 03 02 05 01", "020501")]           // child longer than the OCTET STRING
    [InlineData("04 05 30 03 02 05 01", "3003020501")] // grandchild invalid
    public void Probe_Failure_KeepsRawContent(string hex, string content)
    {
        AssertTree(hex, Octets(content));
    }

    [Fact]
    public void Probe_FailureDoesNotDisturbSiblings()
    {
        AssertTree("30 0A 04 05 68 65 6C 6C 6F 02 01 07", Seq(Octets("68656C6C6F"), Int("07")));
    }

    [Fact]
    public void Probe_NestedProbeFailure_DoesNotFailOuterProbe()
    {
        AssertTree("04 07 30 05 04 03 02 05 01", OctetsWith(Seq(Octets("020501"))));
    }

    [Fact]
    public void Probe_OnlyForUniversalOctetString()
    {
        AssertTree("80 03 02 01 05", Ctx(0x80, "020105"));
        AssertTree("0C 03 02 01 05", Utf8("020105"));
    }

    #endregion

    #region BIT STRING

    [Theory]
    [InlineData("03 02 07 80", "0780")]
    [InlineData("03 01 00", "00")]
    [InlineData("03 04 00 02 01 05", "00020105")] // current behavior: never encapsulates, see KnownQuirkTests
    [InlineData("03 03 06 C0 00", "06C000")]
    public void BitString_IsLeafWithUnusedBitsByte(string hex, string content)
    {
        AssertTree(hex, Bits(content));
    }

    #endregion

    #region Errors

    [Fact]
    public void Empty_Fails() => AssertFails("", "Stream is null or empty.");

    [Fact]
    public void ContainerPastEndOfStream_Fails() =>
        AssertFails("30 05 02 01", "Container at offset 2 has a length of 5, which is past the end of the stream");

    [Fact]
    public void ChildOverrunsContainer_Fails() =>
        AssertFails("30 03 02 02 01 05", "Content size is not correct for container at offset 2");

    [Fact]
    public void PrimitivePastEndOfStream_Fails() =>
        AssertFails("02 05 01", "Content size is not correct for tag Integer at offset 2");

    [Fact]
    public void FailureDeepInside_FailsWholeDocument() =>
        AssertFails("30 08 30 06 30 04 02 05 01 02", "Content size is not correct for tag Integer at offset 8");

    [Fact]
    public void Deserialize_ThrowsWithErrorMessage()
    {
        var ex = Assert.Throws<Asn1DeserializationException>(() => Asn1Serializer.Deserialize(H("30 05 02 01")));
        Assert.Equal("Container at offset 2 has a length of 5, which is past the end of the stream", ex.Message);
    }

    [Fact]
    public void Deserialize_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Asn1Serializer.Deserialize(Array.Empty<byte>()));
    }

    #endregion

    #region Input handling

    [Fact]
    public void TrailingBytesAfterRoot_AreIgnored()
    {
        AssertTree("02 01 05 FF FF", Int("05"));
    }

    [Fact]
    public void Stream_IsReadFromItsCurrentPosition()
    {
        using var ms = new MemoryStream(H("FF FF 30 03 02 01 05"));
        ms.Position = 2;

        Seq(Int("05")).AssertMatches(Asn1Serializer.Deserialize(ms));
        Assert.Equal(7, ms.Position);
    }

    [Fact]
    public void CustomFactory_IsAskedForEveryElementInStreamOrder()
    {
        var factory = new Differential.RecordingFactory(new Factory.DefaultTagFactory());

        Assert.True(Asn1Serializer.TryDeserialize(H("30 08 02 01 05 04 03 02 01 07"), factory, out _, out _));

        Assert.Equal([0x30, 0x02, 0x04, 0x02], factory.Calls);
    }

    [Fact]
    public void CustomFactory_SeesTagsOfAFailedProbe()
    {
        var factory = new Differential.RecordingFactory(new Factory.DefaultTagFactory());

        Assert.True(Asn1Serializer.TryDeserialize(H("30 07 04 02 02 05 02 01 07"), factory, out var tag, out _));

        Assert.Equal([0x30, 0x04, 0x02, 0x02], factory.Calls);
        Seq(Octets("0205"), Int("07")).AssertMatches(tag);
    }

    #endregion
}
