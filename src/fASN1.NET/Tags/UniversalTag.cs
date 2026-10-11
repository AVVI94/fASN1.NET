using System.Collections.Generic;


namespace fASN1.NET.Tags;

/// <summary>
/// A universal tag with an arbitrary number; <see cref="ITag.TagNumber"/> is the given number.
/// </summary>
public class UniversalTag : TagBase
{
    private readonly int _tagNumber;

    /// <summary>Creates the tag for an identifier byte, with the given content or children.</summary>
    /// <param name="tagNumber">The identifier byte, including the class and constructed bits.</param>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public UniversalTag(int tagNumber, byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
        _tagNumber = tagNumber;
    }
    /// <inheritdoc/>
    public override int TagNumber => _tagNumber;
    /// <inheritdoc/>
    public override string TagName => "Universal_" + TagNumber;
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsUniversal => true;
}
