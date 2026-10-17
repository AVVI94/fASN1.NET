using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 UTCTime type (universal 23): a date and time in UTC as <c>YYMMDDHHMMSSZ</c> text. Following
/// RFC 5280, two-digit years 50–99 mean 1950–1999 and 00–49 mean 2000–2049.
/// </summary>
public class UTCTime : TagBase
{
    /// <summary>First year a UTCTime can hold.</summary>
    internal const int MIN_YEAR = 1950;

    /// <summary>Last year a UTCTime can hold.</summary>
    internal const int MAX_YEAR = 2049;

    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public UTCTime(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <summary>Creates the tag holding <paramref name="time"/>, converted to UTC.</summary>
    /// <param name="time">
    /// The date and time. A local time is converted to UTC; a time of unspecified kind is treated as local,
    /// as <see cref="DateTime.ToUniversalTime"/> does.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The year in UTC is outside 1950–2049, which UTCTime cannot represent. Use <see cref="GeneralizedTime"/>.
    /// </exception>
    public UTCTime(DateTime time) : this(Encoding.ASCII.GetBytes(Format(time)), null)
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 23;
    /// <inheritdoc/>
    public override string TagName => "UTCTime";
    /// <inheritdoc/>
    public override int TagClass => 0;

    /// <summary>Formats <paramref name="time"/> as UTCTime text, <c>YYMMDDHHMMSSZ</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The year in UTC is outside 1950–2049.</exception>
    internal static string Format(DateTime time)
    {
        var utc = time.Kind == DateTimeKind.Utc ? time : time.ToUniversalTime();
        if (utc.Year is < MIN_YEAR or > MAX_YEAR)
        {
            throw new ArgumentOutOfRangeException(nameof(time), time,
                $"UTCTime can only hold the years {MIN_YEAR} to {MAX_YEAR}; use GeneralizedTime for other dates.");
        }
        return utc.ToString("yyMMddHHmmss'Z'", CultureInfo.InvariantCulture);
    }

    /// <summary>The four-digit year of a two-digit UTCTime year, by the RFC 5280 rule.</summary>
    internal static int ExpandYear(int twoDigitYear) => twoDigitYear >= MIN_YEAR % 100 ? 1900 + twoDigitYear : 2000 + twoDigitYear;
}
