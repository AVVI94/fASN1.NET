namespace fASN1.NET.Tags;

/// <summary>
/// Universal tags, by the <see cref="ITag.TagNumber"/> their tag classes report. For the constructed types
/// (<see cref="Sequence"/>, <see cref="Set"/>, <see cref="EmbeddedPDV"/>) that is the identifier byte,
/// including the constructed bit 0x20; for the others it is the tag number.
/// </summary>
public enum Tag
{
    /// <summary>End-of-contents marker (universal 0).</summary>
    Eoc = 0,
    /// <summary>BOOLEAN (universal 1).</summary>
    Boolean = 0x01,
    /// <summary>INTEGER (universal 2).</summary>
    Integer = 0x02,
    /// <summary>BIT STRING (universal 3).</summary>
    BitString = 0x03,
    /// <summary>OCTET STRING (universal 4).</summary>
    OctetString = 0x04,
    /// <summary>NULL (universal 5).</summary>
    Null = 0x05,
    /// <summary>OBJECT IDENTIFIER (universal 6).</summary>
    ObjectIdentifier = 0x06,
    /// <summary>ObjectDescriptor (universal 7).</summary>
    ObjectDescriptor = 0x07,
    /// <summary>EXTERNAL (universal 8).</summary>
    External = 0x08,
    /// <summary>REAL (universal 9).</summary>
    Real = 0x09,
    /// <summary>ENUMERATED (universal 10).</summary>
    Enumerated = 0x0A,
    /// <summary>EMBEDDED PDV (universal 11), always constructed.</summary>
    EmbeddedPDV = 0x2B,
    /// <summary>UTF8String (universal 12).</summary>
    UTF8String = 0x0C,
    /// <summary>SEQUENCE (universal 16), always constructed.</summary>
    Sequence = 0x30,
    /// <summary>SET (universal 17), always constructed.</summary>
    Set = 0x31,
    /// <summary>NumericString (universal 18).</summary>
    NumericString = 0x12,
    /// <summary>PrintableString (universal 19).</summary>
    PrintableString = 0x13,
    /// <summary>TeletexString (universal 20).</summary>
    TeletexString = 0x14,
    /// <summary>VideotexString (universal 21).</summary>
    VideotexString = 0x15,
    /// <summary>IA5String (universal 22).</summary>
    IA5String = 0x16,
    /// <summary>UTCTime (universal 23).</summary>
    UTCTime = 0x17,
    /// <summary>GeneralizedTime (universal 24).</summary>
    GeneralizedTime = 0x18,
    /// <summary>GraphicString (universal 25).</summary>
    GraphicString = 0x19,
    /// <summary>VisibleString (universal 26).</summary>
    VisibleString = 0x01A,
    /// <summary>GeneralString (universal 27).</summary>
    GeneralString = 0x1B,
    /// <summary>UniversalString (universal 28).</summary>
    UniversalString = 0x1C,
    /// <summary>CHARACTER STRING (universal 29). The default factory has no class for it.</summary>
    CharacterString = 0x1D,
    /// <summary>BMPString (universal 30).</summary>
    BMPString = 0x1E,
}

/// <summary>
/// Helpers for <see cref="Tag"/>.
/// </summary>
public static class TagEnumExtensions
{
    /// <summary>
    /// Gets the name of a tag as used by <see cref="ITag.TagName"/>. Unlike <see cref="System.Enum.ToString()"/>,
    /// it does not use reflection.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The name, or the number for a value outside the enum.</returns>
    public static string ToString2(this Tag tag)
    {
        return tag switch
        {
            Tag.Eoc => "Eoc",
            Tag.Boolean => "Boolean",
            Tag.Integer => "Integer",
            Tag.BitString => "BitString",
            Tag.OctetString => "OctetString",
            Tag.Null => "Null",
            Tag.ObjectIdentifier => "ObjectIdentifier",
            Tag.ObjectDescriptor => "ObjectDescriptor",
            Tag.External => "External",
            Tag.Real => "Real",
            Tag.Enumerated => "Enumerated",
            Tag.EmbeddedPDV => "EmbeddedPDV",
            Tag.UTF8String => "UTF8String",
            Tag.Sequence => "Sequence",
            Tag.Set => "Set",
            Tag.NumericString => "NumericString",
            Tag.PrintableString => "PrintableString",
            Tag.TeletexString => "TeletexString",
            Tag.VideotexString => "VideotexString",
            Tag.IA5String => "IA5String",
            Tag.UTCTime => "UTCTime",
            Tag.GeneralizedTime => "GeneralizedTime",
            Tag.GraphicString => "GraphicString",
            Tag.VisibleString => "VisibleString",
            Tag.GeneralString => "GeneralString",
            Tag.UniversalString => "UniversalString",
            Tag.CharacterString => "CharacterString",
            Tag.BMPString => "BMPString",
            _ => tag.ToString(),
        };
    }
}
