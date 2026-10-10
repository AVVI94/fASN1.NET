using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 GeneralizedTime type (universal 24): a date and time as YYYYMMDDHHMMSS text.
/// </summary>
public class GeneralizedTime : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public GeneralizedTime(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }
    /// <summary>Creates the tag holding <paramref name="time"/>.</summary>
    /// <param name="time">The date and time.</param>
    public GeneralizedTime(DateTime time) : base(Encode(time))
    {
    }

    private static byte[] Encode(DateTime time)
    {
        var val = time.ToString("yyyyMMddHHmmss.f");
        if (time.Kind == DateTimeKind.Utc)
        {
            val += "Z";
        }
        return Encoding.ASCII.GetBytes(val);
    }

    /// <inheritdoc/>
    public override int TagNumber => 24;
    /// <inheritdoc/>
    public override string TagName => "GeneralizedTime";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
