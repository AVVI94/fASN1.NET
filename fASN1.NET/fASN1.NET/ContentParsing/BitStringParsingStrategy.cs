using System;
using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows BIT STRING content as a string of <c>0</c> and <c>1</c> characters, without the unused bits.
/// </summary>
public class BitStringParsingStrategy : IContentParsingStrategy
{
    internal static string ParseBitString(byte[] data)
    {
        var unusedBits = data[0];
        if (unusedBits > 7)
            throw new Exception($"Invalid BitString with unused bits {unusedBits}");

        var sb = new StringBuilder((data.Length - 1) * 8 - unusedBits);

        for (var i = 1; i < data.Length; ++i)
        {
            var b = data[i];
            var skip = i == data.Length - 1 ? unusedBits : 0;

            for (var j = 7; j >= skip; --j)
            {
                sb.Append((b >> j & 1) == 0 ? '0' : '1');
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc/>

    public string Parse(byte[] content)
    {
        return ParseBitString(content);
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static BitStringParsingStrategy Default { get; } = new BitStringParsingStrategy();
}
