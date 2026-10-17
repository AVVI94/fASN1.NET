using System;
using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// A tag of the application class.
/// </summary>
public class Application : TagBase
{
    private readonly int _tagNumber;
    private readonly bool _isConstructed;

    /// <summary>Creates the tag with the given number, and content or children.</summary>
    /// <param name="tagNumber">The tag number within the class, 0 or more; 31 and up use the multi-byte form.</param>
    /// <param name="isConstructed">Whether the encoding is constructed. A tag with children is constructed either way.</param>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tagNumber"/> is negative.</exception>
    public Application(int tagNumber, bool isConstructed, byte[]? content = null, List<ITag>? children = null) : base(content, children)
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
    public override string TagName => TagNames.Application(_tagNumber);
    /// <inheritdoc/>
    public override int TagClass => 1;
    /// <inheritdoc/>
    public override bool IsConstructed => _isConstructed || base.IsConstructed;
}
