using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;
using static fASN1.NET.Tests.Infrastructure.Expected;

namespace fASN1.NET.Tests;

/// <summary>
/// Hand-written inputs with the complete expected tree written out node by node.
/// Each assertion checks type, TagNumber, TagName, TagClass, IsConstructed, IsUniversal, IsEoc,
/// Content bytes and the ordered children, so no tag can be dropped, added, reordered or
/// mis-typed without failing. Every input is also checked against the public contract (see <see cref="Contract"/>).
/// </summary>
public class ParsedStructureTests
{
    private static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    private static ITag Parse(string hex) => Parse(H(hex));

    private static ITag Parse(byte[] data)
    {
        var (tag, error) = Contract.Check(data);
        Assert.True(tag is not null, error);
        return tag!;
    }

    private static void AssertTree(string hex, Expected expected) => expected.AssertMatches(Parse(hex));

    private static void AssertFails(string hex, string? expectedError)
    {
        var (tag, error) = Contract.Check(H(hex));
        Assert.Null(tag);
        Assert.Equal(expectedError, error);
    }

    #region Tag identity

    public static IEnumerable<object[]> PrimitiveUniversalIdentifiers =>
        TagIdentity.KnownUniversalIdentifiers.Where(id => id is not (0x30 or 0x31 or 0x2B)).Select(id => new object[] { id });

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
    [InlineData(0x0B)] // EMBEDDED PDV is always constructed (0x2B); the primitive form is not a valid encoding
    public void UnassignedUniversal_IsUnknownLeaf(int identifier)
    {
        var tag = Parse([(byte)identifier, 0x01, 0x7F]);

        new Expected(typeof(Unknown), identifier, $"Unknown_{identifier & 0x1F}", 0, false, true, false, [0x7F], []).AssertMatches(tag);
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
    public void EmbeddedPdv_IsConstructedWithNumber11()
    {
        Assert.Equal(11, new EmbeddedPdv().TagNumber);
        AssertTree("2B 03 02 01 05", new Expected(typeof(EmbeddedPdv), 11, "EmbeddedPDV", 0, true, true, false, [], [Int("05")]));
        Assert.Equal(H("2B 03 02 01 05"), Asn1Serializer.Serialize(Parse("2B 03 02 01 05")));
    }

    [Fact]
    public void Application_PrimitiveAndConstructed()
    {
        AssertTree("41 01 07", new Expected(typeof(Application), 1, "Application_1", 1, false, false, false, [0x07], []));
        AssertTree("63 03 02 01 05", new Expected(typeof(Application), 3, "Application_3", 1, true, false, false, [], [Int("05")]));
    }

    [Theory]
    [InlineData("41 01 07")]
    [InlineData("63 03 02 01 05")]
    [InlineData("C2 01 07")]
    [InlineData("E4 03 02 01 05")]
    [InlineData("0D 01 7F")]
    [InlineData("22 03 04 01 41")]
    [InlineData("BE 03 02 01 05")]
    public void NonStandardClasses_SerializeBackToTheSameBytes(string hex)
    {
        Assert.Equal(H(hex), Asn1Serializer.Serialize(Parse(hex)));
    }

    [Fact]
    public void Private_PrimitiveAndConstructed()
    {
        AssertTree("C2 01 07", new Expected(typeof(Private), 2, "Private_2", 3, false, false, false, [0x07], []));
        AssertTree("E4 03 02 01 05", new Expected(typeof(Private), 4, "Private_4", 3, true, false, false, [], [Int("05")]));
    }

    #endregion

    #region Lengths

    [Fact]
    public void ZeroLength_Nodes()
    {
        AssertTree("05 00", Null());
        AssertTree("04 00", Octets(""));
        AssertTree("03 00", Bits(""));
        AssertTree("30 00", new Expected(typeof(Sequence), 16, "Sequence", 0, true, true, false, [], []));
        AssertTree("31 00", new Expected(typeof(Tags.Set), 17, "Set", 0, true, true, false, [], []));
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
    [InlineData("02", "Unexpected end of data in the length of the element at offset 0")]
    [InlineData("30 82", "Unexpected end of data in the length of the element at offset 0")]
    [InlineData("30 03 02 82 01", "Unexpected end of data in the length of the element at offset 2")]
    [InlineData("30 04 02 82 01 00", "Content size is not correct for tag Integer at offset 6")]
    [InlineData("02 87 00 00 00 00 00 00 01 05", "Length of the element at offset 0 uses 7 octets; at most 6 are supported")]
    [InlineData("02 FF 01", "Length of the element at offset 0 uses 127 octets; at most 6 are supported")]
    [InlineData("04 85 01 00 00 00 00", "Length of the element at offset 0 is too large")]
    [InlineData("04 84 80 00 00 00", "Length of the element at offset 0 is too large")]
    [InlineData("04 84 7F FF FF FF 00", "Content size is not correct for tag OctetString at offset 6")]
    [InlineData("30 03 04 85 01 00 00 00 00", "Length of the element at offset 2 is too large")]
    public void InvalidLength_FailsWithMessage(string hex, string message)
    {
        AssertFails(hex, message);
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
    public void Indefinite_InvalidChild_FailsTheDocument()
    {
        // The second INTEGER claims 5 bytes; it must not be dropped quietly.
        AssertFails("30 80 02 01 05 02 05 01 00 00", "Content size is not correct for tag Integer at offset 7");
    }

    [Fact]
    public void Indefinite_MissingEndOfContents_Fails()
    {
        AssertFails("30 80 02 01 05", "Indefinite-length container at offset 2 has no end-of-contents marker");
        AssertFails("30 80 30 80 02 01 05 00 00", "Indefinite-length container at offset 2 has no end-of-contents marker");
    }

    [Fact]
    public void Indefinite_EndOfContentsWithLength_Fails()
    {
        AssertFails("30 80 00 01 05 00 00", "Invalid end-of-contents marker at offset 2");
    }

    [Fact]
    public void Indefinite_EndsOnlyAtEndOfContentsBytes()
    {
        // A constructed INTEGER (0x22) is an Unknown tag; it is an ordinary child and must not end the SEQUENCE.
        var unknown = new Expected(typeof(Unknown), 2, "Unknown_2", 0, true, true, false, [], [Octets("41")]);
        AssertTree("30 80 22 80 04 01 41 00 00 02 01 05 00 00", Seq(unknown, Int("05")));
    }

    [Fact]
    public void Indefinite_PrimitiveTag_Fails()
    {
        AssertFails("02 80 01 00 00", "Cannot skip over an invalid tag with indefinite length");
    }

    [Fact]
    public void Indefinite_PrimitiveOctetString_Fails()
    {
        // BER allows indefinite length only for constructed encodings.
        AssertFails("04 80 02 01 05 00 00", "Cannot skip over an invalid tag with indefinite length");
    }

    [Fact]
    public void Indefinite_EmptyContainer_HasNoContent()
    {
        AssertTree("30 80 00 00 02 01 07", Seq());
        AssertTree("30 80 30 80 00 00 02 01 07 00 00", Seq(Seq(), Int("07")));
        AssertTree("30 80 A0 80 00 00 00 00", Seq(new Expected(typeof(ContextSpecific_0), 0, "[0]", 2, true, false, false, [], [])));
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
    public void Probe_UnknownChildIsNotEoc()
    {
        var unknown = new Expected(typeof(Unknown), 0x0D, "Unknown_13", 0, false, true, false, [0x07], []);
        AssertTree("04 03 0D 01 07", OctetsWith(unknown));
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
    [InlineData("03 04 00 02 01 05", "00020105")] // valid DER inside stays raw: BIT STRING is never expanded
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
    public void MultiByteTagNumber_IsRead()
    {
        AssertTree("1F 21 01 05", new Expected(typeof(Unknown), 33, "Unknown_33", 0, false, true, false, [0x05], []));
        AssertTree("BF 28 03 02 01 05", new Expected(typeof(ContextSpecificTag), 40, "[40]", 2, true, false, false, [], [Int("05")]));
        AssertTree("30 05 5F 87 68 01 05",
            Seq(new Expected(typeof(Application), 1000, "Application_1000", 1, false, false, false, [0x05], [])));
        AssertTree("DF 87 FF FF FF 7F 00", new Expected(typeof(Private), int.MaxValue, $"Private_{int.MaxValue}", 3, false, false, false, [], []));
    }

    [Theory]
    [InlineData("1F 21 01 05")]
    [InlineData("BF 28 03 02 01 05")]
    [InlineData("5F 87 68 01 05")]
    [InlineData("30 07 9F 1F 00 BF 81 00 00")]
    public void MultiByteTagNumber_SerializesBackToTheSameBytes(string hex)
    {
        Assert.Equal(H(hex), Asn1Serializer.Serialize(Parse(hex)));
    }

    [Theory]
    [InlineData("1F 80 21 01 05", "The tag number of the element at offset 0 is not in the shortest form")] // leading zero digit
    [InlineData("1F 1E 01 05", "The tag number of the element at offset 0 is not in the shortest form")]    // 30 fits one byte
    [InlineData("30 03 5F 05 00", "The tag number of the element at offset 2 is not in the shortest form")]
    [InlineData("1F 88 80 80 80 00 00", "The tag number of the element at offset 0 is too large")]          // 2^31
    [InlineData("1F 81", "Unexpected end of data in the identifier of the element at offset 0")]
    [InlineData("1F", "Unexpected end of data in the identifier of the element at offset 0")]
    public void InvalidMultiByteTagNumber_Fails(string hex, string error)
    {
        AssertFails(hex, error);
    }

    [Fact]
    public void MultiByteTagNumber_InsideOctetString_KeepsRawContent()
    {
        AssertTree("04 03 1F 01 05", Octets("1F0105"));
    }

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
    public void Stream_AtItsEnd_FailsWithMessage()
    {
        using var ms = new MemoryStream(H("02 01 05"));
        ms.Position = 3;

        Assert.False(Asn1Serializer.TryDeserialize(ms, out var tag, out var error));
        Assert.Null(tag);
        Assert.Equal("Unexpected end of data at offset 3", error);
    }

    [Fact]
    public void Stream_ReturningShortReads_IsReadCompletely()
    {
        var data = TestCorpus.Get("file:der_09");
        using var stream = new OneBytePerReadStream(data);

        TreeAssert.Equal(Asn1Serializer.Deserialize(data), Asn1Serializer.Deserialize(stream));
    }

    /// <summary>A seekable stream that never returns more than one byte from <see cref="Stream.Read(byte[], int, int)"/>.</summary>
    private sealed class OneBytePerReadStream(byte[] data) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));
    }

    [Fact]
    public void CustomFactory_BuildsEveryNodeOfTheTree()
    {
        var factory = new InstanceTrackingFactory();

        Assert.True(Asn1Serializer.TryDeserialize(H("30 0E 02 01 05 04 03 02 01 07 30 80 05 00 00 00"), factory, out var tag, out _));

        var nodes = TreeAssert.Walk(tag).Select(n => n.Tag).ToList();
        Assert.Equal(6, nodes.Count);
        Assert.All(nodes, node => Assert.Contains(node, factory.Created));
    }

    private sealed class InstanceTrackingFactory : Factory.ITagFactory
    {
        private readonly Factory.DefaultTagFactory _inner = new();

        public HashSet<ITag> Created { get; } = new(ReferenceEqualityComparer.Instance);

        public ITag Create(int tagClass, bool isConstructed, int tagNumber, byte[]? content = null)
        {
            var created = _inner.Create(tagClass, isConstructed, tagNumber, content);
            Created.Add(created);
            return created;
        }
    }

    [Fact]
    public void FactoryException_TryDeserializeReportsIt_DeserializeThrows()
    {
        var data = H("30 06 02 01 05 02 01 07");

        Assert.False(Asn1Serializer.TryDeserialize(data, new Contract.ThrowingFactory(2), out var tag, out var error));
        Assert.Null(tag);
        Assert.False(string.IsNullOrEmpty(error));
        Assert.ThrowsAny<Exception>(() => Asn1Serializer.Deserialize(data, new Contract.ThrowingFactory(2)));
    }

    #endregion
}
