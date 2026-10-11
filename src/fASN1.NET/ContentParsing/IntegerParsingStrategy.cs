using System;
using System.Numerics;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows INTEGER content as a decimal number.
/// </summary>
public class IntegerParsingStrategy : IContentParsingStrategy
{
    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static IntegerParsingStrategy Default { get; } = new IntegerParsingStrategy();

    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        if (content.Length == 0)
        {
            return ""; // invalid: an INTEGER has at least one byte
        }

        return BigInteger.Parse(BitConverter.ToString(content).Replace("-", ""), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture)
                         .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
