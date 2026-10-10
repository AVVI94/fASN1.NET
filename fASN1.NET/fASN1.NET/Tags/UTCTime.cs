using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 UTCTime type (universal 23): a date and time as YYMMDDHHMMSS text.
/// </summary>
public class UTCTime : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public UTCTime(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }
    /// <summary>Creates the tag holding <paramref name="time"/>.</summary>
    /// <param name="time">The date and time.</param>
    public UTCTime(DateTime time) : this(ConvertTime(time), null)
    {
    }

    private static byte[] ConvertTime(DateTime dateTime)
    {
        var str = dateTime.ToString("yyMMddHHmmss");
        if (int.Parse(str.Substring(0, 2)) >= 70)
        {
            str = "19" + str.Substring(2);
        }
        else
        {
            str = "20" + str.Substring(2);
        }
        return Encoding.ASCII.GetBytes(str);
    }

    /// <inheritdoc/>
    public override int TagNumber => 23;
    /// <inheritdoc/>
    public override string TagName => "UTCTime";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
