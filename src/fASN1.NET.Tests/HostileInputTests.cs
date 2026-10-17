using System.Diagnostics;
using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Small inputs crafted to make the parser do disproportionate work.
/// </summary>
public class HostileInputTests
{
    private const int PROBE_COUNT = 40;

    /// <summary>
    /// Each OCTET STRING holds the header of a SEQUENCE that claims all remaining bytes of the input. Without a
    /// bound on children, every failed probe made its parent rescan everything after it, doubling the work per
    /// probe: 22 probes (136 bytes) took about 2 seconds, and 40 would take years.
    /// </summary>
    [Fact]
    public void ProbesClaimingTheRestOfTheInput_ParseInLinearTime()
    {
        // Built from the last probe backwards, so every SEQUENCE header uses a DER length; otherwise the probe
        // would be rejected for its length form before reaching past its OCTET STRING.
        var probes = new List<byte>();
        for (int i = 0; i < PROBE_COUNT; i++)
        {
            byte[] header = [0x30, .. ParsedStructureTests.EncodeLength(probes.Count)];
            probes.InsertRange(0, [0x04, (byte)header.Length, .. header]);
        }
        byte[] data = [0x30, .. ParsedStructureTests.EncodeLength(probes.Count), .. probes];

        var tag = ParseWithin(data, TimeSpan.FromSeconds(2));

        Assert.IsType<Sequence>(tag);
        Assert.Equal(PROBE_COUNT, tag.Children.Count);
        Assert.All(tag.Children.Take(PROBE_COUNT - 1), child =>
        {
            Assert.IsType<OctetString>(child);
            Assert.Empty(child.Children);
            Assert.Equal(0x30, child.Content[0]);
        });
        // The last one claims zero remaining bytes, so it really holds an empty SEQUENCE.
        Assert.IsType<Sequence>(Assert.Single(tag.Children[PROBE_COUNT - 1].Children));
    }

    /// <summary>
    /// The same attack with indefinite-length SEQUENCEs that look for their end-of-contents marker past the end
    /// of the OCTET STRING holding them. Encapsulated children must now have DER lengths, so these probes fail
    /// at the first header; the test keeps guarding the time it takes.
    /// </summary>
    [Fact]
    public void IndefiniteLengthProbesReachingPastTheirOctetString_ParseInLinearTime()
    {
        var data = new List<byte> { 0x30, 0x81, (byte)(PROBE_COUNT * 6) };
        for (int i = 0; i < PROBE_COUNT; i++)
        {
            data.AddRange([0x04, 0x02, 0x30, 0x80]);
        }

        for (int i = 0; i < PROBE_COUNT; i++)
        {
            data.AddRange([0x05, 0x00]);
        }

        var tag = ParseWithin(data.ToArray(), TimeSpan.FromSeconds(2));

        Assert.Equal(PROBE_COUNT * 2, tag.Children.Count);
        Assert.All(tag.Children.Take(PROBE_COUNT), child => Assert.Equal([0x30, 0x80], child.Content));
    }

    [Fact]
    public void ChildCrossingTheEndOfItsContainer_FailsInsteadOfReadingOn()
    {
        // The inner SEQUENCE claims 4 bytes, but the outer one ends after 3 of them.
        var (tag, error) = Contract.Check(Convert.FromHexString("3005300402010500"));

        Assert.Null(tag);
        Assert.Equal("The nested encodings do not match the length of the contents at offset 2", error);
    }

    private static ITag ParseWithin(byte[] data, TimeSpan limit)
    {
        var stopwatch = Stopwatch.StartNew();
        var (tag, error) = Contract.Check(data);
        stopwatch.Stop();

        Assert.True(tag is not null, error);
        Assert.True(stopwatch.Elapsed < limit, $"parsing {data.Length} bytes took {stopwatch.Elapsed.TotalMilliseconds:F0} ms");
        return tag!;
    }
}
