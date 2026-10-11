namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows GeneralizedTime content as ISO 8601 text that does not depend on the current culture, such as
/// <c>2025-01-02T03:04:05.25Z</c>. A time with an offset is converted to UTC; a time without a zone is local time
/// and is shown without the <c>Z</c>. Content that is not a valid GeneralizedTime is shown as
/// <see cref="OctetStringParsingStrategy"/> would show it.
/// </summary>
public class GeneralizedTimeParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
        => Asn1Time.TryParseGeneralizedTime(content, out var value)
            ? Asn1Time.ToDisplayString(value)
            : OctetStringParsingStrategy.Default.Parse(content);

    /// <summary>A shared instance; the strategy keeps no state.</summary>
    public static GeneralizedTimeParsingStrategy Default { get; } = new GeneralizedTimeParsingStrategy();
}
