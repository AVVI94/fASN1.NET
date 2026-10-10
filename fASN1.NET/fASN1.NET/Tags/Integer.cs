using System;
using System.Diagnostics;


namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 INTEGER type (universal 2): a two's-complement, big-endian integer.
/// </summary>
public class Integer : TagBase
{
    /// <summary>Creates the tag with the given content.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    public Integer(byte[]? content = null) : base(content)
    {
    }
    /// <summary>Creates the tag from the big-endian bytes of <paramref name="value"/>, with leading zero bytes removed.</summary>
    /// <param name="value">The value.</param>
    public Integer(int value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    /// <summary>Creates the tag from the big-endian bytes of <paramref name="value"/>, with leading zero bytes removed.</summary>
    /// <param name="value">The value.</param>
    public Integer(long value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    /// <summary>Creates the tag from the big-endian bytes of <paramref name="value"/>, with leading zero bytes removed.</summary>
    /// <param name="value">The value.</param>
    public Integer(short value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    /// <summary>Creates the tag from the big-endian bytes of <paramref name="value"/>, with leading zero bytes removed.</summary>
    /// <param name="value">The value.</param>
    public Integer(uint value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    /// <summary>Creates the tag from the big-endian bytes of <paramref name="value"/>, with leading zero bytes removed.</summary>
    /// <param name="value">The value.</param>
    public Integer(ushort value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    /// <summary>Creates the tag from the big-endian bytes of <paramref name="value"/>, with leading zero bytes removed.</summary>
    /// <param name="value">The value.</param>
    public Integer(ulong value) : this(Convert(BitConverter.GetBytes(value)))
    {
    }
    /// <summary>Creates the tag from the bytes of <see cref="System.Numerics.BigInteger.ToByteArray()"/>, used as they are.</summary>
    /// <param name="value">The value.</param>
    public Integer(System.Numerics.BigInteger value) : this(value.ToByteArray())
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 2;
    /// <inheritdoc/>
    public override string TagName => "Integer";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
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
