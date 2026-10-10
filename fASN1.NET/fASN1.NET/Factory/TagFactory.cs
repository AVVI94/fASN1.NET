using fASN1.NET.Tags;

namespace fASN1.NET.Factory;

/// <summary>
/// Creates the <see cref="ITag"/> for an identifier byte. Pass a custom factory to the deserializer to use your own
/// tag classes. The parser assumes that a tag created for a given identifier always has the same
/// <see cref="ITag.IsConstructed"/>, <see cref="ITag.IsUniversal"/> and <see cref="ITag.TagNumber"/>.
/// </summary>
public interface ITagFactory
{
    /// <summary>
    /// Creates an instance of <see cref="ITag"/> based on the provided tag number.
    /// </summary>
    /// <param name="tag">The tag value.</param>
    /// <param name="content">The content bytes.</param>
    /// <returns>An instance of <see cref="ITag"/>.</returns>
    ITag Create(int tag, byte[]? content = null);
}

/// <summary>
/// Creates the built-in tag class for each identifier byte, and <see cref="Unknown"/> for universal tags it has no class for.
/// </summary>
public sealed class DefaultTagFactory : ITagFactory
{
    /// <summary>The factory is stateless, so one instance serves every call that does not pass its own.</summary>
    internal static DefaultTagFactory Instance { get; } = new();

    /// <inheritdoc/>
    public ITag Create(int tag, byte[]? content = null)
    {
        var cls = tag >> 6;
        return cls switch
        {
            0 => (Tag)tag switch
            {
                Tag.Eoc => new Eoc(),
                Tag.Boolean => new BooleanTag(content: content),
                Tag.Integer => new Integer(content: content),
                Tag.BitString => new BitString(content: content),
                Tag.OctetString => new OctetString(content: content),
                Tag.Null => new Null(),
                Tag.ObjectIdentifier => new ObjectIdentifier(content: content),
                Tag.ObjectDescriptor => new ObjectDescriptor(content: content),
                Tag.External => new External(content: content),
                Tag.Real => new Real(content: content),
                Tag.Enumerated => new Enumerated(content: content),
                Tag.EmbeddedPDV => new EmbeddedPdv(),
                Tag.UTF8String => new Utf8String(content: content),
                Tag.Sequence => new Sequence(),
                Tag.Set => new Set(),
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
                _ => new Unknown(tag)
            },
            1 => new Application(tag),
            2 => (tag & 0x1F) switch
            {
                0 => new ContextSpecific_0(tag),
                1 => new ContextSpecific_1(tag),
                2 => new ContextSpecific_2(tag),
                3 => new ContextSpecific_3(tag),
                _ => new ContextSpecificTag(tag)
            },
            3 => new Private(tag),
            _ => new Unknown(tag)
        };
    }
}
