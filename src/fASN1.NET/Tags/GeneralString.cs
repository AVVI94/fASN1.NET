using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 GeneralString type (universal 27).
/// </summary>
public class GeneralString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public GeneralString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <summary>Creates the tag from text, encoded as ASCII.</summary>
    /// <param name="text">The text.</param>
    public GeneralString(string text) : this(Encoding.ASCII.GetBytes(text))
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 27;
    /// <inheritdoc/>
    public override string TagName => "GeneralString";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
