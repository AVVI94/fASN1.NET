namespace fASN1.NET.ContentParsing;

internal class NullParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        return "";
    }
    public static NullParsingStrategy Default { get; internal set; } = new();
}