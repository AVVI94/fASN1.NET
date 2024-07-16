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
    public static string TagToString(ITag tag, Asn1StringSerializerOptions options)
    {
        EnsureValidOptions(options);
        var sb = new StringBuilder();
        TagToStringInternal(tag, sb, options, 0);
        return sb.ToString();
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

    private static void TagToStringInternal(ITag tag, StringBuilder sb, Asn1StringSerializerOptions options, int level)
    {
        var indent = options.IndentationString.Multiply(level);
        var strategy = options.ContentParsingStrategyLocator;
        var content = GetContentString(tag, options, strategy, options.IndentationString.Multiply(level + 1));
        string tagName = !options.IncludeTagNameForContentTags && content.Length != 0 ? "" : tag.TagName;
        var tagNameAndContentSeparator = options.IncludeTagNameForContentTags && content.Length != 0 ? options.TagNameAndContentSeparator : "";
        var line = options.StringFormat
            .Replace("%IndentationString%", indent)
            .Replace("%TagName%", tagName)
            .Replace("%TagNameAndContentSeparator%", tagNameAndContentSeparator)
            .Replace("%TagContent%", content);
        _ = sb.AppendLine(line);

        if (tag.Children.Count > 0)
        {
            foreach (var child in tag.Children)
            {
                //sb.AppendLine();
                TagToStringInternal(child, sb, options, level + 1);
            }
        }
    }

    private static string GetContentString(ITag tag, Asn1StringSerializerOptions options, StrategyLocator strategyLocator, string wrappedContentIndentation)
    {
        var content = tag.ContentToString(strategyLocator);
        if (options.MaximumContentLineLength < 1)
            return content;
        if (content.Length <= options.MaximumContentLineLength)
            return content;
        if (options.ContentLengthHandling == ContentLengthHandling.Truncate)
            return content.Substring(0, (int)options.MaximumContentLineLength - options.TruncateString.Length) + options.TruncateString;

        string s;
        if (options.ContentLengthHandling == ContentLengthHandling.WordWrap)
        {
            s = WordWrapString(options, content, wrappedContentIndentation);
        }
        s = WrapString(options, content, wrappedContentIndentation);
        if (s.Contains(Environment.NewLine))
        {
            s = Environment.NewLine + s;
        }
        return s;
    }

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
                _ = wrappedContent.AppendLine()
                                  .Append(wrappedContentIndentation)
                                  .Append(content.Substring(start, length));
            }
            start += length;
        }

        return wrappedContent.ToString();
    }

    private static string Multiply(this string s, int n)
    {
        if (n == 0)
            return "";
        if (n == 1)
            return s;
        if (n == 2)
            return s + s;
        if (n == 3)
            return s + s + s;

        return new StringBuilder().Insert(0, s, n).ToString();
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
}