namespace fASN1.NET.ContentParsing;

internal class NullParsingStrategy : IContentParsingStrategy
{
    public static NullParsingStrategy Default { get; internal set; } = new();

    public string Parse(byte[] content)
    {
        return "";
    }
}