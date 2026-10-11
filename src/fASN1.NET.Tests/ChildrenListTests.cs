using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Parsed containers keep their children in an exact-size array until <see cref="ITag.Children"/> is read.
/// These tests pin that the list handed out is still a complete, mutable <see cref="IList{T}"/>.
/// </summary>
public class ChildrenListTests
{
    private static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    [Fact]
    public void ParsedTree_CanBeEditedAndSerialized()
    {
        var tag = Asn1Serializer.Deserialize(H("30 06 02 01 01 02 01 02"));
        var children = tag.Children;

        children.Add(new Integer([3]));
        children.Insert(0, new Null());
        children.RemoveAt(1);                 // INTEGER 1
        children[0] = new BooleanTag([0xFF]); // replaces NULL
        Assert.True(children.Remove(children[1]));

        Assert.Same(children, tag.Children);
        Assert.Equal(H("30 06 01 01 FF 02 01 03"), Asn1Serializer.Serialize(tag));
    }

    [Fact]
    public void ParsedTree_ListOperationsBehaveLikeList()
    {
        var tag = Asn1Serializer.Deserialize(H("30 09 02 01 01 02 01 02 02 01 03"));
        var children = tag.Children;
        var second = children[1];

        Assert.Equal(3, children.Count);
        Assert.False(children.IsReadOnly);
        Assert.Equal(1, children.IndexOf(second));
        Assert.Contains(second, children);
        var copy = new ITag[4];
        children.CopyTo(copy, 1);
        Assert.Same(second, copy[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => children[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => children.Insert(4, new Null()));

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in children)
                children.Add(new Null());
        });

        children.Clear();
        Assert.Empty(children);
        Assert.Equal(H("30 00"), Asn1Serializer.Serialize(tag));
    }

    [Fact]
    public void ParsedLeaf_KeepsContentWhenChildrenAreRead()
    {
        var tag = Asn1Serializer.Deserialize(H("04 02 41 42"));

        Assert.Empty(tag.Children);
        Assert.Equal(H("41 42"), tag.Content);
        Assert.False(tag.IsConstructed);

        tag.Children.Add(new Null());
        Assert.Equal(H("41 42"), tag.Content);
        tag.Content = [];
        Assert.True(tag.IsConstructed);
        Assert.Equal(H("04 02 05 00"), Asn1Serializer.Serialize(tag));
    }

    [Fact]
    public void ParsedContainer_CanAlsoGetContent()
    {
        // An encapsulating OCTET STRING: it reports IsConstructed from what it holds, unlike tags that keep their
        // identifier's constructed bit.
        var tag = Asn1Serializer.Deserialize(H("04 03 02 01 05"));
        Assert.True(tag.IsConstructed);

        tag.Content = [0x01];

        Assert.Single(tag.Children);
        Assert.Equal([0x01], tag.Content);
        Assert.False(tag.IsConstructed);
    }

    [Fact]
    public void ConstructorList_IsUsedByReference()
    {
        var list = new List<ITag> { new Integer([1]) };
        var sequence = new Sequence(list);

        list.Add(new Integer([2]));

        Assert.Same(list, sequence.Children);
        Assert.Equal(2, sequence.Children.Count);

        var octets = new OctetString([0x41], list);
        Assert.Same(list, octets.Children);
        Assert.Equal([0x41], octets.Content);
        octets.Content = [];
        Assert.Same(list, octets.Children);
    }

    [Fact]
    public void ChildrenOfASharedTree_AreTheSameListOnEveryThread()
    {
        for (int round = 0; round < 50; round++)
        {
            var tag = Asn1Serializer.Deserialize(TestCorpus.Get("file:der_09"));
            var leaves = TreeAssert.Walk(tag).Select(n => n.Tag).ToList();
            var seen = new IList<ITag>[leaves.Count, 4];

            Parallel.For(0, 4, thread =>
            {
                for (int i = 0; i < leaves.Count; i++)
                    seen[i, thread] = leaves[i].Children;
            });

            for (int i = 0; i < leaves.Count; i++)
            {
                for (int t = 1; t < 4; t++)
                    Assert.Same(seen[i, 0], seen[i, t]);
            }
        }
    }
}
