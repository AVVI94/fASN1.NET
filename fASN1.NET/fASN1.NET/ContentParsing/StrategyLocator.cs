using System.Collections.Generic;
using fASN1.NET.Tags;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Represents a strategy locator for content parsing.
/// </summary>
public class StrategyLocator
{
    /// <summary>
    /// Gets the dictionary of content parsing strategies.
    /// </summary>
    public Dictionary<int, IContentParsingStrategy> Strategies { get; } = new()
        {
            { (int)Tag.Boolean,          BooleanParsingStrategy.Default },
            { (int)Tag.Integer,          IntegerParsingStrategy.Default },
            { (int)Tag.BitString,        BitStringParsingStrategy.Default },
            { (int)Tag.OctetString,      OctetStringParsingStrategy.Default },
            { (int)Tag.Null,             NullParsingStrategy.Default },
            { (int)Tag.ObjectIdentifier, OidParsingStrategy.Default },
            { (int)Tag.UTF8String,       Utf8StringParsingStrategy.Default },
            { (int)Tag.PrintableString,  IsoStringParsingStrategy.Default },
            { (int)Tag.IA5String,        IsoStringParsingStrategy.Default },
            { (int)Tag.NumericString,    IsoStringParsingStrategy.Default },
            { (int)Tag.GeneralString,    IsoStringParsingStrategy.Default },
            { (int)Tag.VisibleString,    IsoStringParsingStrategy.Default },
            { (int)Tag.UTCTime,          UtcTimeParsingStrategy.Default },
            { (int)Tag.GeneralizedTime,  GeneralizedTimeParsingStrategy.Default },
            { (int)Tag.UniversalString,  UniversalStringParsingStrategy.Default },
            { (int)Tag.BMPString,        BmpStringParsingStrategy.Default },
            { (int)Tag.GraphicString,    Base64ParsingStrategy.Default },
            { (int)Tag.VideotexString,   Base64ParsingStrategy.Default },
            { (int)Tag.TeletexString,    Base64ParsingStrategy.Default },
            { (int)Tag.CharacterString,  IsoStringParsingStrategy.Default }
        };

    /// <summary>
    /// Gets the fallback dictionary of content parsing strategies.
    /// </summary>
    protected IReadOnlyDictionary<int, IContentParsingStrategy> FallbackStrategies { get; } = new Dictionary<int, IContentParsingStrategy>()
        {
            { (int)Tag.Boolean,          BooleanParsingStrategy.Default },
            { (int)Tag.Integer,          IntegerParsingStrategy.Default },
            { (int)Tag.BitString,        BitStringParsingStrategy.Default },
            { (int)Tag.OctetString,      OctetStringParsingStrategy.Default },
            { (int)Tag.Null,             NullParsingStrategy.Default },
            { (int)Tag.ObjectIdentifier, OidParsingStrategy.Default },
            { (int)Tag.UTF8String,       Utf8StringParsingStrategy.Default },
            { (int)Tag.PrintableString,  IsoStringParsingStrategy.Default },
            { (int)Tag.IA5String,        IsoStringParsingStrategy.Default },
            { (int)Tag.NumericString,    IsoStringParsingStrategy.Default },
            { (int)Tag.GeneralString,    IsoStringParsingStrategy.Default },
            { (int)Tag.VisibleString,    IsoStringParsingStrategy.Default },
            { (int)Tag.UTCTime,          UtcTimeParsingStrategy.Default },
            { (int)Tag.GeneralizedTime,  GeneralizedTimeParsingStrategy.Default },
            { (int)Tag.UniversalString,  UniversalStringParsingStrategy.Default },
            { (int)Tag.BMPString,        BmpStringParsingStrategy.Default },
            { (int)Tag.GraphicString,    Base64ParsingStrategy.Default },
            { (int)Tag.VideotexString,   Base64ParsingStrategy.Default },
            { (int)Tag.CharacterString,  IsoStringParsingStrategy.Default }
        };

    /// <summary>
    /// Gets the content parsing strategy for the specified tag.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The content parsing strategy.</returns>
    public virtual IContentParsingStrategy GetStrategy(Tag tag) => GetStrategy((int)tag);

    /// <summary>
    /// Gets the content parsing strategy for the specified tag.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The content parsing strategy.</returns>
    public virtual IContentParsingStrategy GetStrategy(int tag)
    {
        if (Strategies.TryGetValue(tag, out var strategy))
        {
            return strategy;
        }

        if (FallbackStrategies.TryGetValue(tag, out var fallbackStrategy))
        {
            return fallbackStrategy;
        }

        return Base64ParsingStrategy.Default;
    }

    /// <summary>
    /// Gets the default instance of the strategy locator.
    /// </summary>
    public static StrategyLocator Default { get; protected set; } = new StrategyLocator();
}
