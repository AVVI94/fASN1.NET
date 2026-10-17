using System;
using System.Numerics;

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

    /// <inheritdoc cref="Integer(BigInteger)"/>
    public Integer(int value) : this((BigInteger)value)
    {
    }

    /// <inheritdoc cref="Integer(BigInteger)"/>
    public Integer(long value) : this((BigInteger)value)
    {
    }

    /// <inheritdoc cref="Integer(BigInteger)"/>
    public Integer(short value) : this((BigInteger)value)
    {
    }

    /// <inheritdoc cref="Integer(BigInteger)"/>
    public Integer(uint value) : this((BigInteger)value)
    {
    }

    /// <inheritdoc cref="Integer(BigInteger)"/>
    public Integer(ushort value) : this((BigInteger)value)
    {
    }

    /// <inheritdoc cref="Integer(BigInteger)"/>
    public Integer(ulong value) : this((BigInteger)value)
    {
    }

    /// <summary>
    /// Creates the tag holding <paramref name="value"/> in the DER form: two's complement, big-endian, in the
    /// fewest bytes. A positive value whose top bit would be set gets a leading 0x00 byte, so 128 is <c>00 80</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    public Integer(BigInteger value) : this(Encode(value))
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

    private static byte[] Encode(BigInteger value)
    {
        // ToByteArray gives the shortest two's-complement form, least significant byte first.
        var bytes = value.ToByteArray();
        Array.Reverse(bytes);
        return bytes;
    }
}
