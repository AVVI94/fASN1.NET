using System;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 BOOLEAN type (universal 1).
/// </summary>
public class BooleanTag : TagBase
{
    /// <summary>Creates the tag with the given content.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    public BooleanTag(byte[]? content = null) : base(content)
    {
    }
    /// <summary>Creates the tag holding <paramref name="value"/>.</summary>
    /// <param name="value">The value.</param>
    public BooleanTag(bool value) : this(BitConverter.GetBytes(value))
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => 1;
    /// <inheritdoc/>
    public override string TagName => "Boolean";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => false;
}
