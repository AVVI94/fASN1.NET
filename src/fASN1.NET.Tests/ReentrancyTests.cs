using fASN1.NET.Factory;
using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// The parser keeps no state between calls; each call rents its own buffers. These tests parse from
/// inside a parse and from many threads at once.
/// </summary>
public class ReentrancyTests
{
    [Fact]
    public void ParseStartedFromInsideAFactory_DoesNotDisturbTheOuterParse()
    {
        var data = TestCorpus.Get("file:der_09");
        var expected = Asn1Serializer.Deserialize(data);

        var outer = Asn1Serializer.Deserialize(data, new ParsingFactory(TestCorpus.Get("file:cms_signed_ber_indefinite")));

        TreeAssert.Equal(expected, outer);
    }

    [Fact]
    public void ParallelParses_AllProduceTheSameTrees()
    {
        var samples = TestCorpus.All.Values.ToArray();
        var expected = samples.Select(s => Asn1Serializer.Deserialize(s)).ToArray();

        Parallel.For(0, 64, new ParallelOptions { MaxDegreeOfParallelism = 8 }, worker =>
        {
            for (int round = 0; round < 20; round++)
            {
                var i = (worker + round) % samples.Length;
                TreeAssert.Equal(expected[i], Asn1Serializer.Deserialize(samples[i]));
            }
        });
    }

    /// <summary>Parses another document every time it creates a tag.</summary>
    private sealed class ParsingFactory(byte[] other) : ITagFactory
    {
        private readonly DefaultTagFactory _inner = new();

        public ITag Create(int tagClass, bool isConstructed, int tagNumber, byte[]? content = null)
        {
            Assert.True(Asn1Serializer.TryDeserialize(other, out _, out var error), error);
            return _inner.Create(tagClass, isConstructed, tagNumber, content);
        }
    }
}
