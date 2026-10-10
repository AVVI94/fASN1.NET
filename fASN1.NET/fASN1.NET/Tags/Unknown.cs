using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// A universal tag the factory has no class for; <see cref="ITag.TagNumber"/> is the identifier byte.
/// </summary>
public class Unknown : TagBase
{
    private readonly int _tagNumber;

    /// <summary>Creates the tag for an identifier byte, with the given content or children.</summary>
    /// <param name="tagNumber">The identifier byte, including the class and constructed bits.</param>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public Unknown(int tagNumber, byte[]? content = null, List<ITag>? children = null) : base(content, children)
    {
        _tagNumber = tagNumber;
    }
    /// <inheritdoc/>
    public override int TagNumber => _tagNumber;
    /// <inheritdoc/>
    public override string TagName => TagNames.Unknown(_tagNumber);
    /// <inheritdoc/>
    public override int TagClass => _tagNumber >> 6;
    /// <inheritdoc/>
    public override bool IsConstructed => (_tagNumber & 0x20) != 0;
    /// <inheritdoc/>
    public override bool IsEoc => TagClass == 0x00 && TagNumber == 0x00;
}
