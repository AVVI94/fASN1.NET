using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 NumericString type (universal 18): digits and spaces.
/// </summary>
public class NumericString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public NumericString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }
    /// <summary>Creates the tag from text, encoded as ASCII.</summary>
    /// <param name="text">Digits and spaces.</param>
    /// <exception cref="System.ArgumentException">The text contains a character the type does not allow.</exception>
    public NumericString(string text) : this(GetBytes(text))
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 18;
    /// <inheritdoc/>
    public override string TagName => "NumericString";
    /// <inheritdoc/>
    public override int TagClass => 0;

    static byte[] GetBytes(string text)
    {
        var s = text.AsSpan();
        foreach (var c in s)
        {
            if (!char.IsWhiteSpace(c) && !char.IsDigit(c))
            {
                throw new ArgumentException("Invalid character in NumericString, NumericString can only contain digits and white spaces", nameof(text));
            }
        }
        return Encoding.ASCII.GetBytes(text);
    }
}
