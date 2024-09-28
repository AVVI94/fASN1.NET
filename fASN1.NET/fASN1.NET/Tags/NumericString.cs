using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class NumericString : ITag
{
    public NumericString(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public NumericString(string text) : this(GetBytes(text))
    {
    }

    public int TagNumber { get; } = 18;
    public string TagName { get; } = Tag.NumericString.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }

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
