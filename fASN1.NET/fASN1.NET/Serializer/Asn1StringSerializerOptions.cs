using fASN1.NET.ContentParsing;

namespace fASN1.NET;
public enum ContentLengthHandling
{
    Truncate,
    Wrap,
    WordWrap
}

/// <summary>
/// Represents the options for serializing ASN.1 strings.
/// </summary>
public sealed class Asn1StringSerializerOptions
{
    /// <summary>
    /// Gets or sets the string used for indentation. Default value is <c>" | "</c>.
    /// </summary>
    public string IndentationString { get; set; } = " | ";

    /// <summary>
    /// Gets or sets a value indicating whether to include the tag name for content tags. Default value is <c>true</c>.
    /// </summary>
    public bool IncludeTagNameForContentTags { get; set; } = true;

    /// <summary>
    /// Gets or sets the separator between the tag name and content. Default value is <c>" "</c>.
    /// </summary>
    public string TagNameAndContentSeparator { get; set; } = " ";

    /// <summary>
    /// Gets or sets the maximum length of a content line. Default value is <c>128</c>.
    /// Minimum value when <see cref="ContentLengthHandling"/> is <see cref="ContentLengthHandling.WordWrap"/> is <c>10</c>.
    /// </summary>
    public int MaximumContentLineLength { get; set; } = 128;

    /// <summary>
    /// Gets or sets the handling of content length. Default value is <see cref="ContentLengthHandling.Wrap"/>.
    /// </summary>
    public ContentLengthHandling ContentLengthHandling { get; set; } = ContentLengthHandling.Wrap;

    /// <summary>
    /// Gets or sets the string used to indicate truncated content. Default value is <c>"..."</c>.
    /// </summary>
    public string TruncateString { get; set; } = "...";

    /// <summary>
    /// Gets or sets the flag indicating whether or not to convert the BIT_STRING that represents key usage to string values. Default value is <see langword="true"/>.
    /// </summary>
    public bool ConvertKeyUsageBitStringToString { get; set; } = true;

    /// <summary>
    /// Gets or sets the format string for generating the string representation of a tag.
    /// </summary>
    /// <remarks>
    /// The following placeholders can be used:
    /// %IndentationString%,
    /// %TagName%,
    /// %TagNameAndContentSeparator%,
    /// %TagContent%,
    /// <br/><br/>
    /// The default value is <c>"%IndentationString%%TagName%%TagNameAndContentSeparator%%TagContent%"</c>
    /// </remarks>
    public string StringFormat { get; set; } = "%IndentationString%%TagName%%TagNameAndContentSeparator%%TagContent%";

    /// <summary>
    /// Gets or sets the strategy locator for content parsing. Default value is <see cref="StrategyLocator.Default"/>.
    /// </summary>
    public StrategyLocator ContentParsingStrategyLocator { get; set; } = StrategyLocator.Default;

    /// <summary>
    /// The default options for ASN.1 string serialization.
    /// </summary>
    internal static Asn1StringSerializerOptions Default { get; } = new Asn1StringSerializerOptions();
}
