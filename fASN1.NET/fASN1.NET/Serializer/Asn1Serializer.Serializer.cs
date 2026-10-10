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
        // The encoding is the pre-order sequence of every element's header followed, for leaves, by its content.
        // Pass 1 lists the elements in pre-order; pass 2 sizes them bottom-up (children follow their parent in the
        // list, so walking it backwards sizes every child before its parent); pass 3 writes front to back.
        // No pass recurses, so the depth of the tree is limited only by memory.
        var elements = new List<ITag>();
        var parents = new List<int>();
        var pending = new Stack<(ITag Tag, int Parent)>();
        pending.Push((tag, -1));
        while (pending.Count > 0)
        {
            var (current, parent) = pending.Pop();
            var index = elements.Count;
            elements.Add(current);
            parents.Add(parent);
            for (int i = TagBase.GetChildCount(current) - 1; i >= 0; i--)
            {
                pending.Push((TagBase.GetChild(current, i), index));
            }
        }

        var contentLengths = new int[elements.Count];
        for (int i = elements.Count - 1; i >= 0; i--)
        {
            if (TagBase.GetChildCount(elements[i]) == 0)
            {
                contentLengths[i] = elements[i].Content.Length;
            }
            if (parents[i] >= 0)
            {
                contentLengths[parents[i]] = checked(contentLengths[parents[i]] + EncodedSize(contentLengths[i]));
            }
        }

        var result = new byte[EncodedSize(contentLengths[0])];
        var position = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            result[position++] = (byte)elements[i].TagNumber;
            position += WriteLength(contentLengths[i], result, position);
            if (TagBase.GetChildCount(elements[i]) == 0)
            {
                var content = elements[i].Content;
                content.CopyTo(result, position);
                position += content.Length;
            }
        }
        return result;
    }

    private static int EncodedSize(int contentLength) => checked(1 + LengthSize(contentLength) + contentLength);

    private static int LengthSize(int length) => length < 0x80 ? 1 : 1 + LengthOctetCount(length);

    private static int LengthOctetCount(int length) => length > 0xFFFFFF ? 4 : length > 0xFFFF ? 3 : length > 0xFF ? 2 : 1;

    private static int WriteLength(int length, byte[] destination, int offset)
    {
        if (length < 0x80)
        {
            destination[offset] = (byte)length;
            return 1;
        }

        // Long form: 0x80 | number of length octets, then the length big-endian with no leading zeros.
        var octets = LengthOctetCount(length);
        destination[offset] = (byte)(0x80 | octets);
        for (int i = octets; i > 0; i--)
        {
            destination[offset + i] = (byte)length;
            length >>= 8;
        }
        return octets + 1;
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
        var cache = new IndentationCache(options.IndentationString);
        // Pre-order walk with an explicit stack: elements come out in the order a recursive walk would give, so an
        // OBJECT IDENTIFIER still names the elements after it, but a deep tree cannot overflow the call stack.
        var pending = new Stack<(ITag Tag, int Level)>();
        pending.Push((tag, 0));
        while (pending.Count > 0)
        {
            var (current, level) = pending.Pop();
            if (level > 0)
            {
                sb.AppendLine();
            }
            AppendTag(current, sb, options, level, format, cache, ref lastObjectIdentifier);
            for (int i = TagBase.GetChildCount(current) - 1; i >= 0; i--)
            {
                pending.Push((TagBase.GetChild(current, i), level + 1));
            }
        }
        return sb.ToString();
    }

    private static void AppendTag(ITag tag,StringBuilder sb, Asn1StringSerializerOptions options, int level, string format, IndentationCache cache, ref string lastObjectIdentifier)
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
            // A malformed OID names nothing.
            lastObjectIdentifier = Oid.OidEncoder.TryGetString(tag.Content, out var oid) ? oid : "";
        }
    }

    private static string GetContentString(ITag tag, Asn1StringSerializerOptions options, string lastObjectIdentifier, StrategyLocator strategyLocator)
    {
        // The text is for people to read, and the input may be malformed or a custom strategy may fail; a dump of
        // the whole tree must not fail because of one value, so such a value is shown as hexadecimal.
        try
        {
            if (options.ConvertKeyUsageBitStringToString
                        && tag.TagNumber == (int)Tag.BitString
                        && lastObjectIdentifier == Oid.OID.KEY_USAGE)
            {
                return KeyUsage.FromBitStringContent(tag.Content, critical: false).ToString();
            }
            return tag.ContentToString(strategyLocator);
        }
        catch (Exception)
        {
            return OctetStringParsingStrategy.ToHex(tag.Content);
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
        // Without this, an empty indentation still looped once per level, making deep trees quadratic.
        if (s.Length == 0 || n <= 0)
            return "";
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