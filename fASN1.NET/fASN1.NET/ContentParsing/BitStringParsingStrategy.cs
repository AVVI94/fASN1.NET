using System;
using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.ContentParsing;
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

    public string Parse(byte[] content)
    {
        return ParseBitString(content);
    }

    public static BitStringParsingStrategy Default { get; } = new BitStringParsingStrategy();
}
