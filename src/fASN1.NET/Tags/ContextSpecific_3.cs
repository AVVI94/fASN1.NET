using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The context-specific tag [3].
/// </summary>
public class ContextSpecific_3 : TagBase
{
    private readonly bool _isConstructed;

    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="isConstructed">
    /// Whether the encoding is constructed, as for an EXPLICIT tag or an IMPLICIT tag on a SEQUENCE. A tag with
    /// children is constructed either way.
    /// </param>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public ContextSpecific_3(bool isConstructed, byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
        _isConstructed = isConstructed;
    }

    /// <inheritdoc/>
    public override int TagNumber => 3;
    /// <inheritdoc/>
    public override string TagName => "[3]";
    /// <inheritdoc/>
    public override int TagClass => 2;
    /// <inheritdoc/>
    public override bool IsUniversal => false;
    /// <inheritdoc/>
    public override bool IsConstructed => _isConstructed || base.IsConstructed;
}
