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

    [Theory]
    [MemberData(nameof(Names))]
    public void Parse_EveryNodeMatchesOracle(string name)
    {
        var data = TestCorpus.Get(name);
        var oracle = OracleNode.Decode(data);

        Assert.True(Asn1Serializer.TryDeserialize(data, out var tag, out var error), error);

        oracle.AssertMatches(tag);
        Assert.Equal(oracle.CountNodes(), TreeAssert.CountNodes(tag));
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
        var data = TestCorpus.Get(name);
        var tag = Asn1Serializer.Deserialize(data);

        var serialized = Asn1Serializer.Serialize(tag);

        Assert.Equal(Convert.ToHexString(data), Convert.ToHexString(serialized));
    }

    [Theory]
    [MemberData(nameof(DerNames))]
    public void Parse_SerializeParse_GivesIdenticalTree(string name)
    {
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
    public void Parse_SatisfiesPublicContract(string name)
    {
        Contract.Check(TestCorpus.Get(name));
    }
}
