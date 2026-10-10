using System.Collections.Generic;


namespace fASN1.NET.Tags;

/// <summary>
/// The context-specific tag [0]; <see cref="ITag.TagNumber"/> is the identifier byte.
/// </summary>
public class ContextSpecific_0 : TagBase
{
    private readonly int _tagNumber;

    /// <summary>Creates the tag for an identifier byte, with the given content or children.</summary>
    /// <param name="tagNumber">The identifier byte, including the class and constructed bits.</param>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public ContextSpecific_0(int tagNumber, byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
        _tagNumber = tagNumber;
    }
    /// <inheritdoc/>
    public override int TagNumber => _tagNumber;
    /// <inheritdoc/>
    public override string TagName => "[0]";
    /// <inheritdoc/>
    public override int TagClass => 2;
    /// <inheritdoc/>
    public override bool IsUniversal => false;
}
