using fASN1.NET.Factory;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Whole-document checks on real and generated certificates, requests and CMS data.
/// Every node of the parsed tree is compared against an oracle built with System.Formats.Asn1,
/// so a dropped, extra, reordered or mis-typed tag fails the test with the path of the node.
/// </summary>
public class CorpusStructureTests
{
    public static IEnumerable<object[]> Names => TestCorpus.Names;
    public static IEnumerable<object[]> DerNames => TestCorpus.DerNames;

    /// <summary>
    /// Samples the oracle cannot describe yet because of a known parser bug.
    /// </summary>
    private static readonly Dictionary<string, string> KnownOracleMismatches = new()
    {
        ["file:cms_signed_ber_indefinite"] =
            "Contains a constructed OCTET STRING (0x24); the parser builds an Unknown tag that reports IsEoc=true " +
            "and ends the enclosing indefinite-length container early. See KnownQuirkTests.UnknownUniversalTag_IsReportedAsEoc.",
    };

    [Theory]
    [MemberData(nameof(Names))]
    public void Parse_EveryNodeMatchesOracle(string name)
    {
        if (KnownOracleMismatches.ContainsKey(name))
            return; // covered by the dedicated quirk test, and by the differential tests
        var data = TestCorpus.Get(name);
        var oracle = OracleNode.Decode(data);

        Assert.True(Asn1Serializer.TryDeserialize(data, out var tag, out var error), error);

        oracle.AssertMatches(tag);
        Assert.Equal(oracle.CountNodes(), TreeAssert.CountNodes(tag));
    }

    /// <summary>
    /// DER samples that cannot round-trip because of a known serializer-side bug.
    /// </summary>
    private static readonly Dictionary<string, string> KnownSerializationMismatches = new()
    {
        ["syn:der_mixed"] =
            "Contains Application and Private tags, whose TagNumber is left at 0, so Serialize writes identifier 0x00. " +
            "See KnownQuirkTests.ApplicationPrivateUnknown_TagNumberIsZero.",
    };

    [Fact]
    public void KnownSerializationMismatches_StillMismatch()
    {
        foreach (var name in KnownSerializationMismatches.Keys)
        {
            var data = TestCorpus.Get(name);
            var serialized = Asn1Serializer.Serialize(Asn1Serializer.Deserialize(data));
            Assert.False(data.AsSpan().SequenceEqual(serialized), $"{name} now round-trips; remove it from {nameof(KnownSerializationMismatches)}.");
        }
    }

    [Fact]
    public void KnownOracleMismatches_StillMismatch()
    {
        // When a known bug is fixed this test fails, as a reminder to remove the entry above.
        foreach (var name in KnownOracleMismatches.Keys)
        {
            var data = TestCorpus.Get(name);
            var oracle = OracleNode.Decode(data);
            var ok = Asn1Serializer.TryDeserialize(data, out var tag, out _);
            var matches = ok && Record.Exception(() => oracle.AssertMatches(tag!)) is null;
            Assert.False(matches, $"{name} now matches the oracle; remove it from {nameof(KnownOracleMismatches)}.");
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Parse_TreeIsWellFormed(string name)
    {
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get(name));
        TreeAssert.WellFormed(tag);
    }

    [Theory]
    [MemberData(nameof(DerNames))]
    public void Serialize_ReproducesDerInputExactly(string name)
    {
        if (KnownSerializationMismatches.ContainsKey(name))
            return;
        var data = TestCorpus.Get(name);
        var tag = Asn1Serializer.Deserialize(data);

        var serialized = Asn1Serializer.Serialize(tag);

        Assert.Equal(Convert.ToHexString(data), Convert.ToHexString(serialized));
    }

    [Theory]
    [MemberData(nameof(DerNames))]
    public void Parse_SerializeParse_GivesIdenticalTree(string name)
    {
        if (KnownSerializationMismatches.ContainsKey(name))
            return;
        var first = Asn1Serializer.Deserialize(TestCorpus.Get(name));
        var second = Asn1Serializer.Deserialize(Asn1Serializer.Serialize(first));
        TreeAssert.Equal(first, second);
    }

    [Theory]
    [MemberData(nameof(DerNames))]
    public void Parse_EveryTruncatedPrefixFails(string name)
    {
        var data = TestCorpus.Get(name);
        for (int length = 1; length < data.Length; length++)
        {
            var ok = Asn1Serializer.TryDeserialize(data.AsSpan(0, length).ToArray(), out var tag, out var error);
            Assert.False(ok, $"prefix of {length}/{data.Length} bytes parsed as {tag?.TagName}");
            // error can be null for prefixes cut inside the length octets; see KnownQuirkTests.
            Assert.True(error is not null || length <= 4, $"prefix of {length} bytes failed without an error message");
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Parse_TrailingBytesDoNotChangeTheTree(string name)
    {
        if (KnownOracleMismatches.ContainsKey(name))
            return; // the early-terminated container reads into the appended bytes
        var data = TestCorpus.Get(name);
        var expected = Asn1Serializer.Deserialize(data);

        var actual = Asn1Serializer.Deserialize([.. data, 0x30, 0x03, 0x02, 0x01, 0x05, 0xFF]);

        TreeAssert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Parse_StreamOverloadMatchesByteArrayOverload(string name)
    {
        var data = TestCorpus.Get(name);
        using var ms = new MemoryStream(data);

        TreeAssert.Equal(Asn1Serializer.Deserialize(data), Asn1Serializer.Deserialize(ms, new DefaultTagFactory()));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Parse_MatchesReferenceImplementation(string name)
    {
        Differential.AssertSameAsReference(TestCorpus.Get(name));
    }
}
