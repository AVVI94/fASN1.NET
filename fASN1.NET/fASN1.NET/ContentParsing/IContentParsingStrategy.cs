namespace fASN1.NET.ContentParsing;

public interface IContentParsingStrategy
{
    public string Parse(byte[] content);
}
