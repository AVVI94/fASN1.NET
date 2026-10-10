using System;
using System.Diagnostics;


namespace fASN1.NET.Tags;

public class Integer : TagBase
{
    public Integer(byte[]? content = null) : base(content)
    {
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

    public override int TagNumber => 2;
    public override string TagName => "Integer";
    public override int TagClass => 0;
    public override bool IsConstructed => false;

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
