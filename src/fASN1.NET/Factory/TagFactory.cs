using fASN1.NET.Tags;

namespace fASN1.NET.Factory;

/// <summary>
/// Creates the <see cref="ITag"/> for an identifier. Pass a custom factory to the deserializer to use your own
/// tag classes. The parser assumes that a tag created for a given identifier always has the same
/// <see cref="ITag.IsConstructed"/>, <see cref="ITag.TagClass"/> and <see cref="ITag.TagNumber"/>. It removes any
/// <see cref="ITag.Content"/> or <see cref="ITag.Children"/> the returned tag already has and fills them from the
/// encoding, so a factory may return pre-filled tags without changing the parsed tree.
/// </summary>
public interface ITagFactory
{
    /// <summary>
    /// Creates the tag for an identifier.
    /// </summary>
    /// <param name="tagClass">The class: 0 universal, 1 application, 2 context-specific, 3 private.</param>
    /// <param name="isConstructed">Whether the identifier has the constructed bit.</param>
    /// <param name="tagNumber">The tag number within the class, 0 or more.</param>
    /// <param name="content">The content bytes, if known; the parser always passes <see langword="null"/> and sets the content itself.</param>
    /// <returns>An instance of <see cref="ITag"/>.</returns>
    ITag Create(int tagClass, bool isConstructed, int tagNumber, byte[]? content = null);
}

/// <summary>
/// Creates the built-in tag class for each identifier, and <see cref="Unknown"/> for universal tags it has no class
/// for: universal numbers without a class here, and the forms a type cannot have (a primitive SEQUENCE, a constructed
/// INTEGER, a constructed string).
/// </summary>
public sealed class DefaultTagFactory : ITagFactory
{
    /// <summary>The factory is stateless, so one instance serves every call that does not pass its own.</summary>
    internal static DefaultTagFactory Instance { get; } = new();

    /// <inheritdoc/>
    public ITag Create(int tagClass, bool isConstructed, int tagNumber, byte[]? content = null)
    {
        return tagClass switch
        {
            0 when isConstructed => (Tag)tagNumber switch
            {
                Tag.Sequence => new Sequence(),
                Tag.Set => new Set(),
                Tag.EmbeddedPDV => new EmbeddedPdv(),
                _ => new Unknown(tagClass, tagNumber, isConstructed, content),
            },
            0 => (Tag)tagNumber switch
            {
                Tag.Eoc => new Eoc(),
                Tag.Boolean => new BooleanTag(content: content),
                Tag.Integer => new Integer(content: content),
                Tag.BitString => new BitString(content, includesUnusedBits: true),
                Tag.OctetString => new OctetString(content: content),
                Tag.Null => new Null(),
                Tag.ObjectIdentifier => new ObjectIdentifier(content: content),
                Tag.ObjectDescriptor => new ObjectDescriptor(content: content),
                Tag.External => new External(content: content),
                Tag.Real => new Real(content: content),
                Tag.Enumerated => new Enumerated(content: content),
                Tag.UTF8String => new Utf8String(content: content),
                Tag.NumericString => new NumericString(content: content),
                Tag.PrintableString => new PrintableString(content: content),
                Tag.TeletexString => new TeletexString(content: content),
                Tag.VideotexString => new VideotexString(content: content),
                Tag.IA5String => new IA5String(content: content),
                Tag.UTCTime => new UTCTime(content: content),
                Tag.GeneralizedTime => new GeneralizedTime(content: content),
                Tag.GraphicString => new GraphicString(content: content),
                Tag.VisibleString => new VisibleString(content: content),
                Tag.GeneralString => new GeneralString(content: content),
                Tag.UniversalString => new UniversalString(content: content),
                Tag.BMPString => new BMPString(content: content),
                _ => new Unknown(tagClass, tagNumber, isConstructed, content),
            },
            1 => new Application(tagNumber, isConstructed, content),
            2 => tagNumber switch
            {
                0 => new ContextSpecific_0(isConstructed, content),
                1 => new ContextSpecific_1(isConstructed, content),
                2 => new ContextSpecific_2(isConstructed, content),
                3 => new ContextSpecific_3(isConstructed, content),
                _ => new ContextSpecificTag(tagNumber, isConstructed, content),
            },
            3 => new Private(tagNumber, isConstructed, content),
            _ => new Unknown(tagClass, tagNumber, isConstructed, content),
        };
    }
}
