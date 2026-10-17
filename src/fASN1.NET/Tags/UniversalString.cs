using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 UniversalString type (universal 28): text in UTF-32.
/// </summary>
public class UniversalString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public UniversalString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <summary>Creates the tag from text, encoded as UTF-32.</summary>
    /// <param name="text">The text.</param>
    public UniversalString(string text) : this(Encoding.UTF32.GetBytes(text))
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 28;
    /// <inheritdoc/>
    public override string TagName => "UniversalString";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
