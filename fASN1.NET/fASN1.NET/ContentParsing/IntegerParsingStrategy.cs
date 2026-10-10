using System;
using System.Numerics;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows INTEGER content as a decimal number.
/// </summary>
public class IntegerParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        return BigInteger.Parse(BitConverter.ToString(content).Replace("-", ""), System.Globalization.NumberStyles.HexNumber).ToString();
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static IntegerParsingStrategy Default { get; } = new IntegerParsingStrategy();
}
