using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace fASN1.NET.ContentParsing;
public class IntegerParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        return BigInteger.Parse(BitConverter.ToString(content).Replace("-", ""), System.Globalization.NumberStyles.HexNumber).ToString();
    }

    public static IntegerParsingStrategy Default { get; } = new IntegerParsingStrategy();
}
