using System;
using System.Globalization;
using fASN1.NET.Tags;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Reads and writes the content of UTCTime and GeneralizedTime (X.680 §46–47) without depending on the current
/// culture. A time with a zone (<c>Z</c> or an offset) is returned in UTC with <see cref="DateTimeKind.Utc"/>; a
/// GeneralizedTime without one is local time and is returned with <see cref="DateTimeKind.Unspecified"/>.
/// </summary>
internal static class Asn1Time
{
    /// <summary>Parses UTCTime: <c>YYMMDDhhmm[ss]</c> followed by <c>Z</c> or <c>+hhmm</c>/<c>-hhmm</c>.</summary>
    /// <remarks>Two-digit years follow RFC 5280: 50–99 are 19xx, 00–49 are 20xx.</remarks>
    public static bool TryParseUtcTime(byte[] content, out DateTime value)
    {
        value = default;
        int pos = 0;
        if (!TryReadDigits(content, ref pos, 2, out var year)
            || !TryReadDigits(content, ref pos, 2, out var month)
            || !TryReadDigits(content, ref pos, 2, out var day)
            || !TryReadDigits(content, ref pos, 2, out var hour)
            || !TryReadDigits(content, ref pos, 2, out var minute))
        {
            return false;
        }
        var second = 0;
        if (pos < content.Length && IsDigit(content[pos]) && !TryReadDigits(content, ref pos, 2, out second))
        {
            return false;
        }

        // UTCTime always has a zone, and an offset always has minutes.
        if (!TryReadZone(content, ref pos, requireMinutes: true, out var offset) || offset is null || pos != content.Length)
        {
            return false;
        }

        return TryCreate(UTCTime.ExpandYear(year), month, day, hour, minute, second, 0, offset, out value);
    }

    /// <summary>
    /// Parses GeneralizedTime: <c>YYYYMMDDhh[mm[ss]]</c>, an optional fraction of the last unit after <c>.</c> or
    /// <c>,</c>, then optionally <c>Z</c> or <c>+hh[mm]</c>/<c>-hh[mm]</c>.
    /// </summary>
    public static bool TryParseGeneralizedTime(byte[] content, out DateTime value)
    {
        value = default;
        int pos = 0;
        if (!TryReadDigits(content, ref pos, 4, out var year)
            || !TryReadDigits(content, ref pos, 2, out var month)
            || !TryReadDigits(content, ref pos, 2, out var day)
            || !TryReadDigits(content, ref pos, 2, out var hour))
        {
            return false;
        }

        int minute = 0, second = 0;
        long unit = TimeSpan.TicksPerHour;
        if (pos < content.Length && IsDigit(content[pos]))
        {
            if (!TryReadDigits(content, ref pos, 2, out minute))
            {
                return false;
            }

            unit = TimeSpan.TicksPerMinute;
            if (pos < content.Length && IsDigit(content[pos]))
            {
                if (!TryReadDigits(content, ref pos, 2, out second))
                {
                    return false;
                }

                unit = TimeSpan.TicksPerSecond;
            }
        }

        long fractionTicks = 0;
        if (pos < content.Length && content[pos] is (byte)'.' or (byte)',')
        {
            pos++;
            var start = pos;
            decimal fraction = 0, scale = 1;
            while (pos < content.Length && IsDigit(content[pos]))
            {
                if (pos - start < 20) // beyond this the digits are far below one tick
                {
                    scale /= 10;
                    fraction += (content[pos] - '0') * scale;
                }
                pos++;
            }
            if (pos == start)
            {
                return false;
            }

            fractionTicks = (long)(fraction * unit);
        }

        if (!TryReadZone(content, ref pos, requireMinutes: false, out var offset) || pos != content.Length)
        {
            return false;
        }

        return TryCreate(year, month, day, hour, minute, second, fractionTicks, offset, out value);
    }

    /// <summary>Parses the content of a UTCTime or GeneralizedTime element.</summary>
    public static bool TryParse(ITag tag, out DateTime value)
    {
        value = default;
        return tag.Children.Count == 0 && tag.TagClass == 0 && tag.TagNumber switch
        {
            (int)Tag.UTCTime => TryParseUtcTime(tag.Content, out value),
            (int)Tag.GeneralizedTime => TryParseGeneralizedTime(tag.Content, out value),
            _ => false,
        };
    }

    /// <summary>
    /// Formats a parsed time as ISO 8601 for display: <c>2025-01-02T03:04:05Z</c>, with a fraction only when there
    /// is one, and without the <c>Z</c> for a local time.
    /// </summary>
    public static string ToDisplayString(DateTime value)
    {
        var text = value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + Fraction(value);
        return value.Kind == DateTimeKind.Utc ? text + "Z" : text;
    }

    /// <summary>
    /// Formats <paramref name="time"/> as DER GeneralizedTime: UTC, <c>YYYYMMDDhhmmss[.f…]Z</c>, the fraction
    /// without trailing zeros and left out when zero.
    /// </summary>
    public static string FormatGeneralizedTime(DateTime time)
    {
        var utc = time.Kind == DateTimeKind.Utc ? time : time.ToUniversalTime();
        return utc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + Fraction(utc) + "Z";
    }

    private static string Fraction(DateTime value)
    {
        var ticks = value.Ticks % TimeSpan.TicksPerSecond;
        return ticks == 0 ? "" : "." + ticks.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
    }

    private static bool TryReadZone(byte[] content, ref int pos, bool requireMinutes, out TimeSpan? offset)
    {
        offset = null;
        if (pos == content.Length)
        {
            return true;
        }

        if (content[pos] == 'Z')
        {
            pos++;
            offset = TimeSpan.Zero;
            return true;
        }
        if (content[pos] is not ((byte)'+' or (byte)'-'))
        {
            return false;
        }

        var negative = content[pos] == '-';
        pos++;
        if (!TryReadDigits(content, ref pos, 2, out var hours) || hours > 23)
        {
            return false;
        }

        var minutes = 0;
        if ((requireMinutes || pos < content.Length) && (!TryReadDigits(content, ref pos, 2, out minutes) || minutes > 59))
        {
            return false;
        }

        var span = new TimeSpan(hours, minutes, 0);
        offset = negative ? -span : span;
        return true;
    }

    private static bool TryCreate(int year, int month, int day, int hour, int minute, int second, long fractionTicks,
                                  TimeSpan? offset, out DateTime value)
    {
        value = default;
        if (year < 1 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)
            || hour > 23 || minute > 59 || second > 59)
        {
            return false;
        }

        var local = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified).AddTicks(fractionTicks);
        if (offset is null)
        {
            value = local;
            return true;
        }

        // The written time is local time at the given offset; UTC is that time minus the offset.
        var utcTicks = local.Ticks - offset.Value.Ticks;
        if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks)
        {
            return false;
        }

        value = new DateTime(utcTicks, DateTimeKind.Utc);
        return true;
    }

    private static bool TryReadDigits(byte[] content, ref int pos, int count, out int value)
    {
        value = 0;
        if (pos + count > content.Length)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            var b = content[pos + i];
            if (!IsDigit(b))
            {
                return false;
            }

            value = (value * 10) + (b - '0');
        }
        pos += count;
        return true;
    }

    private static bool IsDigit(byte b) => b is >= (byte)'0' and <= (byte)'9';
}
