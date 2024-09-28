using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace fASN1.NET.Tags;

public class Integer : ITag
{
    public Integer(byte[]? content = null)
    {
        Content = content ?? [];
        Children = new List<ITag>();
    }
    public Integer(int value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    public Integer(long value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    public Integer(short value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    public Integer(uint value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    public Integer(ushort value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    public Integer(ulong value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    public Integer(System.Numerics.BigInteger value) : this(value.ToByteArray())
    {
    }

    public int TagNumber { get; } = 2;
    public string TagName { get; } = Tag.Integer.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed { get; } = false;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }

    private static byte[] Convert(byte[] value)
    {
        var span = value.AsSpan();
        if (BitConverter.IsLittleEndian)
        {
            span.Reverse(); 
        }

        bool allZero = true;
        int i = -1;
        foreach (var c in span)
        {
            i++;
            if (c != 0)
            {
                allZero = false;
                break;
            }
        }
        if (allZero)
        {
            return [0];
        }
        Debug.Assert(i < span.Length);
        return span.Slice(i).ToArray();
    }
}
