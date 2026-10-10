using System.Collections.Generic;


namespace fASN1.NET.Tags;

/// <summary>
/// A context-specific tag with a number of 4 or more; <see cref="ITag.TagNumber"/> is the identifier byte.
/// </summary>
public class ContextSpecificTag : TagBase
{
    private readonly int _tagNumber;

    /// <summary>Creates the tag for an identifier byte, with the given content or children.</summary>
    /// <param name="tagNumber">The identifier byte, including the class and constructed bits.</param>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public ContextSpecificTag(int tagNumber, byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
        _tagNumber = tagNumber;
    }
    /// <inheritdoc/>
    public override int TagNumber => _tagNumber;
    /// <inheritdoc/>
    public override string TagName => TagNames.ContextSpecific(_tagNumber);
    /// <inheritdoc/>
    public override int TagClass => 2;
    /// <inheritdoc/>
    public override bool IsUniversal => false;
}
