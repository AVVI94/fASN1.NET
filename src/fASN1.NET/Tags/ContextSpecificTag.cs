using System;
using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// A context-specific tag [n]; the classes <see cref="ContextSpecific_0"/> to <see cref="ContextSpecific_3"/> cover [0] to [3].
/// </summary>
public class ContextSpecificTag : TagBase
{
    private readonly int _tagNumber;
    private readonly bool _isConstructed;

    /// <summary>Creates the tag with the given number, and content or children.</summary>
    /// <param name="tagNumber">The tag number within the class, 0 or more; 31 and up use the multi-byte form.</param>
    /// <param name="isConstructed">Whether the encoding is constructed. A tag with children is constructed either way.</param>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tagNumber"/> is negative.</exception>
    public ContextSpecificTag(int tagNumber, bool isConstructed, byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
        if (tagNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tagNumber), tagNumber, "A tag number cannot be negative.");
        }

        _tagNumber = tagNumber;
        _isConstructed = isConstructed;
    }

    /// <inheritdoc/>
    public override int TagNumber => _tagNumber;
    /// <inheritdoc/>
    public override string TagName => TagNames.ContextSpecific(_tagNumber);
    /// <inheritdoc/>
    public override int TagClass => 2;
    /// <inheritdoc/>
    public override bool IsUniversal => false;
    /// <inheritdoc/>
    public override bool IsConstructed => _isConstructed || base.IsConstructed;
}
