using System;
using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.ContentParsing;
public class BooleanParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        return content.Length == 1 && content[0] == 0xff ? "True" : "False";
    }

    public static BooleanParsingStrategy Default { get; } = new BooleanParsingStrategy();
}
