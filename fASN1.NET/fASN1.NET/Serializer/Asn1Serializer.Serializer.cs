using System;
using System.Collections.Generic;
using System.Text;
using fASN1.NET.ContentParsing;
using fASN1.NET.Tags;

namespace fASN1.NET;
public static partial class Asn1Serializer
{
    #region Serialize
    /// <summary>
    /// Serializes the specified ASN.1 tag into a byte array.
    /// </summary>
    /// <param name="tag">The ASN.1 tag to serialize.</param>
    /// <returns>The byte array representation of the serialized ASN.1 tag.</returns>
    public static byte[] Serialize(ITag tag)
    {
        var res = new List<byte>();
        SerializeInternal(tag, res);
        return [.. res];
    }

    private static void SerializeInternal(ITag tag, List<byte> res)
    {
        var res2 = new List<byte>();
        res.Add((byte)tag.TagNumber);
        if (tag.Children.Count > 0)
        {
            foreach (var child in tag.Children)
            {
                SerializeInternal(child, res2);
            }
        }
        else
        {
            res2.AddRange(tag.Content);
        }
        res.AddRange(EncodeLength(res2.Count));
        res.AddRange(res2);
    }

    static byte[] EncodeLength(int length)
    {
        if (length < 128)
            return [(byte)length];
        var r = BitConverter.GetBytes(length);
        int l = 0;
#pragma warning disable IDE2001 // Embedded statements must be on their own line
        while (r[++l] != 0) ;
#pragma warning restore IDE2001 // Embedded statements must be on their own line
        var res = new byte[l + 1];
        res[l] = (byte)(l + 128);
        Array.Copy(r, res, l);
        Array.Reverse(res);
        return res;
    }
    #endregion

    /// <summary>
    /// Converts the specified ASN.1 tag to its string representation using the default options.
    /// </summary>
    /// <param name="tag">The ASN.1 tag to convert.</param>
    /// <returns>The string representation of the ASN.1 tag.</returns>
    public static string TagToString(ITag tag)
    {
        return TagToString(tag, Asn1StringSerializerOptions.Default);
    }

    /// <summary>
    /// Converts the specified ASN.1 tag to its string representation using the provided options.
    /// </summary>
    /// <param name="tag">The ASN.1 tag to convert.</param>
    /// <param name="options">The options for serializing the ASN.1 tag.</param>
    /// <returns>The string representation of the ASN.1 tag.</returns>
    public static string TagToString(ITag tag, Action<Asn1StringSerializerOptions> options)
    {
        var o = new Asn1StringSerializerOptions();
        options(o);
        return TagToString(tag, o);
    }

    /// <summary>
    /// Converts the specified ASN.1 tag to its string representation using the provided options.
    /// </summary>
    /// <param name="tag">The ASN.1 tag to convert.</param>
    /// <param name="options">The options for serializing the ASN.1 tag.</param>
    /// <returns>The string representation of the ASN.1 tag.</returns>
    public static string TagToString(ITag tag, Asn1StringSerializerOptions options)
    {
        EnsureValidOptions(options);
        var sb = new StringBuilder();
        var format = options.StringFormat
            .Replace("%IndentationString%", "{0}")
            .Replace("%TagName%", "{1}")
            .Replace("%TagNameAndContentSeparator%", "{2}")
            .Replace("%TagContent%", "{3}");

        var lastObjectIdentifier = "";
        TagToStringInternal(tag, sb, options, 0, format, new IndentationCache(options.IndentationString), ref lastObjectIdentifier);
        return sb.ToString();
    }


    private static void TagToStringInternal(ITag tag, StringBuilder sb, Asn1StringSerializerOptions options, int level, string format, IndentationCache cache, ref string lastObjectIdentifier)
    {
        var indent = cache[level];
        var strategyLocator = options.ContentParsingStrategyLocator;

        var content = ProcessContentString(GetContentString(tag,
                                                            options,
                                                            lastObjectIdentifier,
                                                            strategyLocator),
                                                  options,
                                                  cache[level + 1] /*indent + options.IndentationString*/);

        string tagName = !options.IncludeTagNameForContentTags && content.Length != 0 ? "" : tag.TagName;
        var tagNameAndContentSeparator = options.IncludeTagNameForContentTags && content.Length != 0 ? options.TagNameAndContentSeparator : "";

        _ = sb.AppendFormat(format, indent, tagName, tagNameAndContentSeparator, content);

        if (tag.TagNumber == (int)Tag.ObjectIdentifier)
        {
            lastObjectIdentifier = Oid.OidEncoder.GetString(tag.Content);
        }

        if (tag.Children.Count > 0)
        {
            foreach (var child in tag.Children)
            {
                sb.AppendLine();
                TagToStringInternal(child, sb, options, level + 1, format, cache, ref lastObjectIdentifier);
            }
        }
    }

    private static string GetContentString(ITag tag, Asn1StringSerializerOptions options, string lastObjectIdentifier, StrategyLocator strategyLocator)
    {
        string content;
        if (options.ConvertKeyUsageBitStringToString
                    && tag.TagNumber == (int)Tag.BitString
                    && lastObjectIdentifier == Oid.OID.KEY_USAGE)
        {
            var ku = new KeyUsage(ByteToBoolArray(tag.Content[0], out var decOnly),
                                  false,
                                  decOnly);
            content = ku.ToString();
        }
        else
        {
            content = tag.ContentToString(strategyLocator);
        }

        return content;

        static bool[] ByteToBoolArray(byte keyUsage, out bool decOnly)
        {
            bool[] keyUsages = new bool[8];
            decOnly = true;
            
            for (int i = 0; i < 8; i++)
            {
                var k = keyUsages[i] = (keyUsage & (1 << i)) != 0;
                decOnly &= !k;
            }

            return keyUsages;
        }
    }

    private static string ProcessContentString(string content, Asn1StringSerializerOptions options, string wrappedContentIndentation)
    {
        if (options.MaximumContentLineLength < 1)
            return content;
        if (content.Length <= options.MaximumContentLineLength)
            return content;
        if (options.ContentLengthHandling == ContentLengthHandling.Truncate)
            //return TruncateString(content, options.TruncateString, options.MaximumContentLineLength); - the bottom is faster lol
            return content.Substring(0, (int)options.MaximumContentLineLength - options.TruncateString.Length) + options.TruncateString;

        var s = options.ContentLengthHandling switch
        {
            ContentLengthHandling.Wrap => WrapString(options, content, wrappedContentIndentation),
            ContentLengthHandling.WordWrap => WordWrapString(options, content, wrappedContentIndentation),
            _ => throw new ArgumentOutOfRangeException(nameof(options.ContentLengthHandling))
        };

        if (s.AsSpan().Contains(Environment.NewLine.AsSpan(), StringComparison.CurrentCulture)
            || s.AsSpan().StartsWith(wrappedContentIndentation.AsSpan()))
        {
            s = Environment.NewLine + s;
        }
        return s;
    }

    //private static unsafe string TruncateString(string content, string truncate, int length)
    //{
    //    var str = new string(char.MinValue, length + truncate.Length);
    //    fixed (char* p = str)
    //    {
    //        var sp = content.AsSpan();
    //        var spn = new Span<char>(p, length);
    //        sp.Slice(0, length).CopyTo(spn);
    //        truncate.AsSpan().CopyTo(spn.Slice(length));
    //    }
    //    return str;
    //}

    private static string WordWrapString(Asn1StringSerializerOptions options, string content, string wrappedContentIndentation)
    {
        var wrappedContent = new StringBuilder();
        var words = content.Split(' ');
        var currentLine = new StringBuilder(wrappedContentIndentation);

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > options.MaximumContentLineLength)
            {
                if (currentLine.Length > wrappedContentIndentation.Length)
                {
                    _ = wrappedContent.AppendLine(currentLine.ToString());
                    _ = currentLine.Clear().Append(wrappedContentIndentation);
                }
            }

            if (currentLine.Length > wrappedContentIndentation.Length)
            {
                _ = currentLine.Append(' ');
            }

            _ = currentLine.Append(word);
        }

        if (currentLine.Length > wrappedContentIndentation.Length)
        {
            _ = wrappedContent.Append(currentLine.ToString());
        }

        return wrappedContent.ToString();
    }

    private static string WrapString(Asn1StringSerializerOptions options, string content, string wrappedContentIndentation)
    {
        var wrappedContent = new StringBuilder();
        int start = 0;

        while (start < content.Length)
        {
            int length = Math.Min((int)options.MaximumContentLineLength, content.Length - start);
            if (start != 0)
            {
                wrappedContent.AppendLine()
                              .Append(wrappedContentIndentation)
                              .Append(content, start, length);
            }
            else
            {
                wrappedContent.Append(wrappedContentIndentation)
                              .Append(content, start, length);
            }
            start += length;
        }

        return wrappedContent.ToString();
    }

    private static string Multiply(this string s, int n)
    {
        return M(s, n);
        //return n switch
        //{
        //    0 => "",
        //    1 => s,
        //    2 => s + s,
        //    3 => s + s + s,
        //    _ => M(s, n) //new StringBuilder(s.Length * n).Insert(0, s, n).ToString()
        //};

        static unsafe string M(string s, int n)
        {
            var str = new string(char.MinValue, s.Length * n);
            var sp = s.AsSpan();
            fixed (char* p = str)
            {
                var spn = new Span<char>(p, str.Length);
                for (int i = 0; i < n; i++)
                {
                    sp.CopyTo(spn.Slice(i * s.Length));
                }
            }
            return str;
        }
    }

    private static void EnsureValidOptions(Asn1StringSerializerOptions options)
    {
        if (options.ContentLengthHandling == ContentLengthHandling.WordWrap && options.MaximumContentLineLength < 10) //common word length in english is about 5 characters
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Maximum content line length must be greater than 9 when using WordWrap.");
        }
        if (options.StringFormat.Contains("%TagContent%") == false)
        {
            throw new ArgumentException("String format must contain %TagContent% placeholder.", nameof(options.StringFormat));
        }
        if (options.IndentationString is null || options.TagNameAndContentSeparator is null || options.TruncateString is null)
        {
            throw new ArgumentException("IndentationString, TagNameAndContentSeparator and/or TruncateString cannot be null.", nameof(options));
        }
    }

    class IndentationCache(string indentationString)
    {
        private readonly Dictionary<int, string> _c = [];
        private readonly string _indentationString = indentationString;

        public string this[int level] => GetIndentationString(level);

        public string GetIndentationString(int level)
        {
            if (!_c.TryGetValue(level, out var indentation))
            {
                _c[level] = indentation = _indentationString.Multiply(level);
            }
            return indentation;
        }
    }
}