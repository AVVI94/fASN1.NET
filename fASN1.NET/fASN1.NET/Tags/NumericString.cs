using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class NumericString : TagBase
{
    public NumericString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }
    public NumericString(string text) : this(GetBytes(text))
    {
    }

    public override int TagNumber => 18;
    public override string TagName => "NumericString";
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
