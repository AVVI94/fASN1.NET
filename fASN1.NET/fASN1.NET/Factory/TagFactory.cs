using fASN1.NET.Tags;
using System;
using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Factory;

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

public sealed class DefaultTagFactory : ITagFactory
{
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
