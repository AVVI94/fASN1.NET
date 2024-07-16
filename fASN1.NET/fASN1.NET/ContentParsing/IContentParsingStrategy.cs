using fASN1.NET.Tags;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fASN1.NET.ContentParsing;

public interface IContentParsingStrategy
{
    public string Parse(byte[] content);
}
