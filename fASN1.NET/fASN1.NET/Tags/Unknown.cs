using System;
using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// A tag the factory has no class for, such as a universal type without a class here or a constructed string
/// (universal 4 constructed).
/// </summary>
public class Unknown : TagBase
{
    private readonly int _tagNumber;
    private readonly byte _tagClass;
    private readonly bool _isConstructed;

    /// <summary>Creates the tag with the given identity, and content or children.</summary>
    /// <param name="tagClass">The class: 0 universal, 1 application, 2 context-specific, 3 private.</param>
    /// <param name="tagNumber">The tag number within the class, 0 or more.</param>
    /// <param name="isConstructed">Whether the encoding is constructed. A tag with children is constructed either way.</param>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The class is not 0 to 3, or the tag number is negative.</exception>
    public Unknown(int tagClass, int tagNumber, bool isConstructed, byte[]? content = null, List<ITag>? children = null) : base(content, children)
    {
        if (tagClass is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(tagClass), tagClass, "The tag class is 0 to 3.");
        if (tagNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(tagNumber), tagNumber, "A tag number cannot be negative.");
        _tagClass = (byte)tagClass;
        _tagNumber = tagNumber;
        _isConstructed = isConstructed;
    }

    /// <inheritdoc/>
    public override int TagNumber => _tagNumber;
    /// <inheritdoc/>
    public override string TagName => TagNames.Unknown(_tagNumber);
    /// <inheritdoc/>
    public override int TagClass => _tagClass;
    /// <summary>The identifier had the constructed bit, whether or not the tag has children now.</summary>
    internal bool HasConstructedIdentifier => _isConstructed;
    /// <inheritdoc/>
    public override bool IsConstructed => _isConstructed || base.IsConstructed;
    /// <inheritdoc/>
    public override bool IsEoc => _tagClass == 0 && _tagNumber == 0 && !_isConstructed;
}
