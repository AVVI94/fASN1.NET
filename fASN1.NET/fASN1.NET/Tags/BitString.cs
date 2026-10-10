using System.Collections.Generic;


namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 BIT STRING type (universal 3). The first content byte is the number of unused bits in the last byte.
/// </summary>
public class BitString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public BitString(byte[]? content = null, IList<ITag>? children = null) : base(WithUnusedBits(content), children)
    {
    }
    /// <summary>Creates the tag from one byte of bits; the unused-bits count is derived from its trailing zero bits.</summary>
    /// <param name="content">The bits.</param>
    public BitString(byte content) : this([content], null) { }

    /// <inheritdoc/>
    public override int TagNumber => 3;
    /// <inheritdoc/>
    public override string TagName => "BitString";
    /// <inheritdoc/>
    public override int TagClass => 0;

    private static byte[]? WithUnusedBits(byte[]? content)
    {
        if (content is null)
            return null;
        var _ = CalculateUnusedBits(content);
        return [(byte)_, .. content];
    }

    /// <summary>
    /// Gets the number of trailing zero bits in the last byte, which the BIT STRING constructors use as the
    /// unused-bits count. A last byte of 0 counts as having no unused bits.
    /// </summary>
    /// <param name="bitStringBytes">The bits, without an unused-bits byte; must not be empty.</param>
    /// <returns>A number from 0 to 7.</returns>
    public static int CalculateUnusedBits(byte[] bitStringBytes)
    {
        byte lastByte = bitStringBytes[bitStringBytes.Length - 1];
        int unusedBits = 0;
        for (int i = 0; i < 8; i++)
        {
            if ((lastByte & 1 << i) != 0)
            {
                unusedBits = 7 - i;
                break;
            }
        }
        return unusedBits;
    }
}
