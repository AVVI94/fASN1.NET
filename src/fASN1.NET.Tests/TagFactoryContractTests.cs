using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Tests;

/// <summary>
/// What a custom tag factory decides and what it does not: it chooses the classes, but the shape of the tree comes
/// from the encoding (X.690), and it is asked for a tag's name only when a message needs one.
/// </summary>
public class TagFactoryContractTests
{
    [Fact]
    public void ShapeComesFromTheEncoding_NotFromTheFactorysTags()
    {
        // The factory makes every tag claim to be constructed; a primitive INTEGER is still a leaf with its content.
        var tag = Asn1Serializer.Deserialize(Convert.FromHexString("3003020105"), new ConstructedClaimingFactory());

        var integer = Assert.Single(tag.Children);
        Assert.Empty(integer.Children);
        Assert.Equal([0x05], integer.Content);
    }

    [Fact]
    public void ErrorMessages_NameTagsAsTheFactoryDoes()
    {
        // INTEGER claiming 5 content bytes with 1 left.
        Assert.False(Asn1Serializer.TryDeserialize(Convert.FromHexString("020505"), new ConstructedClaimingFactory(), out _, out var error));

        Assert.Equal("The contents of Custom_2 at offset 2 reach past the end of the data", error);
    }

    [Fact]
    public void Factory_IsOnlyAskedForTheTagsOfTheTree()
    {
        var factory = new CountingFactory();

        Asn1Serializer.Deserialize(Convert.FromHexString("3006020105020107"), factory);

        Assert.Equal(3, factory.Calls); // SEQUENCE and two INTEGERs; scanning asks for nothing
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrefilledTags_AreEmptiedBeforeTheParserFillsThem(bool customTags)
    {
        // An INTEGER, an encapsulating OCTET STRING, an empty SEQUENCE, a constructed OCTET STRING, an empty SEQUENCE in
        // the indefinite form and an OCTET STRING whose contents are not encodings.
        var data = Convert.FromHexString("301F" + "020105" + "04053003020107" + "3000" + "24800401410401420000" + "30800000" + "0403020500");

        var expected = Asn1Serializer.Deserialize(data);
        var actual = Asn1Serializer.Deserialize(data, new PrefillingFactory(customTags));

        Assert.Equal(Shape(expected), Shape(actual));
        if (!customTags)
        {
            Assert.Equal(Asn1Serializer.Serialize(expected), Asn1Serializer.Serialize(actual));
        }
    }

    private static string Shape(ITag tag)
        => $"{tag.TagNumber}:{Convert.ToHexString(tag.Content)}[{string.Join(",", tag.Children.Select(Shape))}]";

    private sealed class ConstructedClaimingFactory : ITagFactory
    {
        public ITag Create(int tagClass, bool isConstructed, int tagNumber, byte[]? content = null)
            => new ClaimingTag(tagNumber, content);
    }

    private sealed class ClaimingTag(int tagNumber, byte[]? content) : TagBase(content)
    {
        public override int TagNumber => tagNumber;
        public override string TagName => $"Custom_{tagNumber}";
        public override int TagClass => 0;
        public override bool IsConstructed => true;
    }

    private sealed class CountingFactory : ITagFactory
    {
        private readonly DefaultTagFactory _inner = new();

        public int Calls { get; private set; }

        public ITag Create(int tagClass, bool isConstructed, int tagNumber, byte[]? content = null)
        {
            Calls++;
            return _inner.Create(tagClass, isConstructed, tagNumber, content);
        }
    }

    /// <summary>Returns tags that already have contents and a child, as built-in tags or as <see cref="MockTag"/>.</summary>
    private sealed class PrefillingFactory(bool customTags) : ITagFactory
    {
        private readonly DefaultTagFactory _inner = new();

        public ITag Create(int tagClass, bool isConstructed, int tagNumber, byte[]? content = null)
        {
            if (customTags)
            {
                return new MockTag { TagNumber = tagNumber, Content = [0xEE], Children = [new Null()] };
            }

            var tag = _inner.Create(tagClass, isConstructed, tagNumber, content);
            tag.Children.Add(new Null());
            tag.Content = [0xEE];
            return tag;
        }
    }
}
