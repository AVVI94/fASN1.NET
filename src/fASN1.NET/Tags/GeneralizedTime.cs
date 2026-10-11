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
    /// <summary>
    /// Creates the tag holding <paramref name="time"/> in the DER form RFC 5280 requires: UTC,
    /// <c>YYYYMMDDHHMMSS[.f…]Z</c>, with a fraction of a second only when there is one and without trailing zeros.
    /// </summary>
    /// <param name="time">
    /// The date and time. A local time is converted to UTC; a time of unspecified kind is treated as local,
    /// as <see cref="DateTime.ToUniversalTime"/> does.
    /// </param>
    public GeneralizedTime(DateTime time) : base(Encoding.ASCII.GetBytes(ContentParsing.Asn1Time.FormatGeneralizedTime(time)))
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 24;
    /// <inheritdoc/>
    public override string TagName => "GeneralizedTime";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
