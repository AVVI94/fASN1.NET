using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Nesting far beyond what a recursive parser survives. The recursive implementation overflowed the
/// call stack at roughly 1,200 levels of indefinite-length nesting (measured on .NET 10, Release, 1 MB
/// main-thread stack): about 5 KB of input was enough to terminate the process, because a stack
/// overflow cannot be caught. The iterative parser only needs heap memory. The trees are inspected without recursion.
/// </summary>
public class DeepNestingTests
{
    private const int DEPTH = 200_000;

    [Fact]
    public void DeepIndefiniteNesting_ParsesEveryLevel()
    {
        // 30 80 × depth, INTEGER 5, 00 00 × depth
        var data = new byte[(DEPTH * 4) + 3];
        for (int i = 0; i < DEPTH; i++)
        {
            data[i * 2] = 0x30;
            data[(i * 2) + 1] = 0x80;
        }
        data[DEPTH * 2] = 0x02;
        data[(DEPTH * 2) + 1] = 0x01;
        data[(DEPTH * 2) + 2] = 0x05;

        Assert.True(Asn1Serializer.TryDeserialize(data, out var tag, out var error), error);

        AssertChain<Sequence>(tag, DEPTH);
    }

    [Fact]
    public void DeepDefiniteNesting_ParsesEveryLevel()
    {
        var data = BuildDefinite(DEPTH, 0x30, [0x02, 0x01, 0x05]);

        Assert.True(Asn1Serializer.TryDeserialize(data, out var tag, out var error), error);

        AssertChain<Sequence>(tag, DEPTH);
    }

    [Fact]
    public void DeepEncapsulatedOctetStrings_ParseEveryLevel()
    {
        var data = BuildDefinite(DEPTH, 0x04, [0x02, 0x01, 0x05]);

        Assert.True(Asn1Serializer.TryDeserialize(data, out var tag, out var error), error);

        AssertChain<OctetString>(tag, DEPTH);
    }

    [Fact]
    public void DeepNesting_FailureAtTheBottom_FailsWithoutCrashing()
    {
        var data = BuildDefinite(DEPTH, 0x30, [0x02, 0x01, 0x05]);
        // Innermost element becomes 05 02 05: a NULL claiming 2 content bytes with only 1 left.
        data[^3] = 0x05;
        data[^2] = 0x02;

        Assert.False(Asn1Serializer.TryDeserialize(data, out _, out var error));
        Assert.Equal($"Content size is not correct for tag Null at offset {data.Length - 1}", error);
    }

    [Fact]
    public void DeepNesting_ProbeFailureAtTheBottom_RollsBackOnlyTheInnermostProbe()
    {
        // Innermost OCTET STRING holds "hello", which is not DER; every outer level still encapsulates.
        var data = BuildDefinite(DEPTH, 0x04, [0x04, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F]);

        Assert.True(Asn1Serializer.TryDeserialize(data, out var tag, out var error), error);

        var leaf = AssertChain<OctetString>(tag, DEPTH);
        Assert.IsType<OctetString>(leaf);
        Assert.Equal("68656C6C6F", Convert.ToHexString(leaf.Content));
    }

    /// <summary>
    /// Wraps <paramref name="innermost"/> in <paramref name="depth"/> definite-length elements.
    /// </summary>
    private static byte[] BuildDefinite(int depth, byte identifier, byte[] innermost)
    {
        // Lengths of each level, from the inside out, so the buffer can be written front to back.
        var lengths = new long[depth];
        long size = innermost.Length;
        for (int i = depth - 1; i >= 0; i--)
        {
            lengths[i] = size;
            size += 1 + ParsedStructureTests.EncodeLength(checked((int)size)).Length;
        }

        var data = new byte[size];
        int pos = 0;
        for (int i = 0; i < depth; i++)
        {
            data[pos++] = identifier;
            foreach (var b in ParsedStructureTests.EncodeLength((int)lengths[i]))
                data[pos++] = b;
        }
        innermost.CopyTo(data, pos);
        return data;
    }

    /// <summary>
    /// Follows the single-child chain and returns the node below the last <typeparamref name="T"/>.
    /// </summary>
    private static ITag AssertChain<T>(ITag root, int depth) where T : ITag
    {
        var node = root;
        for (int level = 0; level < depth; level++)
        {
            Assert.True(node is T, $"level {level} is {node.GetType().Name}");
            Assert.True(node.Children.Count == 1, $"level {level} has {node.Children.Count} children");
            Assert.Empty(node.Content);
            node = node.Children[0];
        }
        Assert.Empty(node.Children);
        if (node is Integer)
            Assert.Equal([0x05], node.Content);
        return node;
    }
}
