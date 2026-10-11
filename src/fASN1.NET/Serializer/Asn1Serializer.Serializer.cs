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
    /// <remarks>
    /// <para>
    /// Lengths are written in the definite form with the fewest octets, as DER requires, and the components of every
    /// SET in DER order (X.690 §11.6): ascending by their encodings, compared as octet strings with the shorter one
    /// padded with zero octets. Without a schema a SET cannot be told from a SET OF, so a SET with fixed fields is
    /// ordered the same way; this matches the tag order of §10.3 unless two components differ only in being primitive
    /// or constructed. A tree parsed from DER serializes back to the same octets.
    /// </para>
    /// <para>
    /// The identifier octets are built from <see cref="ITag.TagClass"/> and <see cref="ITag.TagNumber"/>, in the
    /// high-tag-number form for numbers of 31 and up. A tag with children is encoded constructed, except a universal
    /// BIT STRING or OCTET STRING, whose children are the encodings it encapsulates as its value; a BIT STRING then gets
    /// an unused-bits octet of 0. A tag without children is encoded constructed when <see cref="ITag.IsConstructed"/>
    /// says so.
    /// </para>
    /// </remarks>
    /// <param name="tag">The ASN.1 tag to serialize.</param>
    /// <returns>The byte array representation of the serialized ASN.1 tag.</returns>
    /// <exception cref="ArgumentException">
    /// A tag has a class outside 0 to 3 or a negative tag number, or has both contents and children.
    /// </exception>
    public static byte[] Serialize(ITag tag)
    {
        // The encoding is, in pre-order, every tag's identifier and length octets followed, for a primitive encoding, by
        // its contents octets. Pass 1 lists the tags in pre-order; pass 2 computes the lengths bottom-up (children
        // follow their parent in the list, so walking it backwards measures every child before its parent); pass 3
        // writes front to back.
        // No pass recurses, so the depth of the tree is limited only by memory.
        var tags = new List<ITag>();
        var identifierOctets = new List<byte>();
        var tagNumbers = new List<int>();
        var parents = new List<int>();
        var contentsLengths = new List<int>();
        var pending = new Stack<(ITag Tag, int Parent)>();
        pending.Push((tag, -1));
        while (pending.Count > 0)
        {
            var (current, parent) = pending.Pop();
            var index = tags.Count;
            var childCount = TagBase.GetChildCount(current);
            if (childCount > 0 && current.Content.Length > 0)
            {
                throw new ArgumentException(
                    $"{current.TagName} has both contents and children; an encoding has contents octets or nested encodings, not both.", nameof(tag));
            }

            var identifierOctet = FirstIdentifierOctet(current, childCount > 0);
            tags.Add(current);
            identifierOctets.Add(identifierOctet);
            tagNumbers.Add(current.TagNumber);
            parents.Add(parent);
            contentsLengths.Add(childCount > 0 ? (identifierOctet == BIT_STRING ? 1 : 0) : current.Content.Length);
            for (int i = childCount - 1; i >= 0; i--)
            {
                pending.Push((TagBase.GetChild(current, i), index));
            }
        }

        var encodingLengths = new int[tags.Count];
        for (int i = tags.Count - 1; i >= 0; i--)
        {
            encodingLengths[i] = checked(IdentifierOctetCount(tagNumbers[i]) + LengthOctetCount(contentsLengths[i]) + contentsLengths[i]);
            if (i > 0)
                contentsLengths[parents[i]] = checked(contentsLengths[parents[i]] + encodingLengths[i]);
        }

        var result = new byte[encodingLengths[0]];
        var encodingStarts = new int[tags.Count];
        var position = 0;
        for (int i = 0; i < tags.Count; i++)
        {
            encodingStarts[i] = position;
            position += WriteIdentifierOctets(identifierOctets[i], tagNumbers[i], result, position);
            position += WriteLengthOctets(contentsLengths[i], result, position);
            if (TagBase.GetChildCount(tags[i]) == 0)
            {
                var contents = tags[i].Content;
                contents.CopyTo(result, position);
                position += contents.Length;
            }
            else if (identifierOctets[i] == BIT_STRING)
            {
                result[position++] = 0x00; // no unused bits: the encapsulated encodings are whole octets
            }
        }
        SortSets(result, identifierOctets, parents, encodingStarts, encodingLengths, contentsLengths);
        return result;
    }

    /// <summary>
    /// Puts the components of every SET in <paramref name="encoding"/> in DER order by moving their encodings, which
    /// lie side by side in the SET's contents octets. Tags are in pre-order, so walking SETs from the last one handles
    /// every SET inside another before the one enclosing it; reordering inside a component does not move the component.
    /// </summary>
    private static void SortSets(byte[] encoding, List<byte> identifierOctets, List<int> parents, int[] encodingStarts, int[] encodingLengths, List<int> contentsLengths)
    {
        Dictionary<int, List<int>>? setChildren = null;
        for (int i = 1; i < identifierOctets.Count; i++)
        {
            if (identifierOctets[parents[i]] != SET)
                continue;
            setChildren ??= [];
            if (!setChildren.TryGetValue(parents[i], out var children))
                setChildren[parents[i]] = children = [];
            children.Add(i);
        }
        if (setChildren is null)
            return;

        var sets = new List<int>(setChildren.Keys);
        sets.Sort((a, b) => b.CompareTo(a));
        foreach (var set in sets)
        {
            var children = setChildren[set];
            var ranges = new (int Start, int Length)[children.Count];
            var isSorted = true;
            for (int i = 0; i < children.Count; i++)
            {
                ranges[i] = (encodingStarts[children[i]], encodingLengths[children[i]]);
                if (i > 0 && CompareEncodings(encoding, ranges[i - 1], ranges[i]) > 0)
                    isSorted = false;
            }
            if (isSorted)
                continue;

            Array.Sort(ranges, (a, b) => CompareEncodings(encoding, a, b));
            var sorted = new byte[contentsLengths[set]];
            var offset = 0;
            foreach (var (start, length) in ranges)
            {
                Buffer.BlockCopy(encoding, start, sorted, offset, length);
                offset += length;
            }
            Buffer.BlockCopy(sorted, 0, encoding, encodingStarts[children[0]], sorted.Length);
        }
    }

    /// <summary>
    /// Compares two encodings as X.690 §11.6 orders SET OF components: as octet strings, the shorter one padded at
    /// its end with zero octets.
    /// </summary>
    internal static int CompareEncodings(byte[] octets, (int Start, int Length) a, (int Start, int Length) b)
    {
        var common = Math.Min(a.Length, b.Length);
        for (int i = 0; i < common; i++)
        {
            var difference = octets[a.Start + i] - octets[b.Start + i];
            if (difference != 0)
                return difference;
        }
        // The longer one is greater exactly when its remaining octets are not all padding zeros.
        var (longer, sign) = a.Length > b.Length ? (a, 1) : (b, -1);
        for (int i = common; i < longer.Length; i++)
        {
            if (octets[longer.Start + i] != 0)
                return sign;
        }
        return 0;
    }

    private const byte BIT_STRING = 0x03;
    private const byte SET = 0x31;
    private const byte CONSTRUCTED_FLAG = 0x20;
    private const int TAG_NUMBER_MASK = 0x1F;

    /// <summary>The first identifier octet of <paramref name="tag"/>; see <see cref="Serialize"/>.</summary>
    private static byte FirstIdentifierOctet(ITag tag, bool hasChildren)
    {
        var tagClass = tag.TagClass;
        var number = tag.TagNumber;
        if (tagClass is < 0 or > 3 || number < 0)
        {
            throw new ArgumentException(
                $"{tag.TagName} has the class {tagClass} and tag number {number}; the class must be 0 to 3 and the number 0 or more.", nameof(tag));
        }

        // A universal BIT STRING or OCTET STRING with children holds them as its encapsulated value, so it stays
        // primitive, unless it was created constructed: an Unknown tag created with the constructed bit stays so.
        var isConstructed = hasChildren
            ? !(tagClass == 0 && number is (int)Tag.BitString or (int)Tag.OctetString) || tag is Unknown { HasConstructedIdentifier: true }
            : tag.IsConstructed;
        return (byte)((tagClass << 6) | (isConstructed ? CONSTRUCTED_FLAG : 0) | Math.Min(number, TAG_NUMBER_MASK));
    }

    /// <summary>Number of identifier octets: one, or in the high-tag-number form one more per 7 bits of the number.</summary>
    private static int IdentifierOctetCount(int tagNumber)
    {
        if (tagNumber < TAG_NUMBER_MASK)
            return 1;
        var size = 2;
        while ((tagNumber >>= 7) != 0)
            size++;
        return size;
    }

    /// <summary>
    /// Writes the identifier octets: the first, then in the high-tag-number form the number in base 128, most
    /// significant digit first, with bit 8 set on every subsequent octet but the last (X.690 §8.1.2.4).
    /// </summary>
    private static int WriteIdentifierOctets(byte first, int tagNumber, byte[] destination, int offset)
    {
        destination[offset] = first;
        var size = IdentifierOctetCount(tagNumber);
        for (int i = size - 1; i > 0; i--)
        {
            destination[offset + i] = (byte)((tagNumber & 0x7F) | (i == size - 1 ? 0 : 0x80));
            tagNumber >>= 7;
        }
        return size;
    }

    /// <summary>Number of length octets in the definite form with the fewest octets (X.690 §10.1).</summary>
    private static int LengthOctetCount(int length) => length < 0x80 ? 1 : 1 + LongFormOctetCount(length);

    /// <summary>Number of subsequent length octets in the long form.</summary>
    private static int LongFormOctetCount(int length) => length > 0xFFFFFF ? 4 : length > 0xFFFF ? 3 : length > 0xFF ? 2 : 1;

    /// <summary>Writes the length octets: the short form below 128, otherwise the long form (§8.1.3.4, §8.1.3.5).</summary>
    private static int WriteLengthOctets(int length, byte[] destination, int offset)
    {
        if (length < 0x80)
        {
            destination[offset] = (byte)length;
            return 1;
        }

        // Long form: bit 8 set and the number of subsequent octets, then the length, most significant octet first.
        var octets = LongFormOctetCount(length);
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
        // Pre-order walk with an explicit stack: tags come out in the order a recursive walk would give, so an
        // OBJECT IDENTIFIER still names the tags after it, but a deep tree cannot overflow the call stack.
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

        if (tag.Is(Tag.ObjectIdentifier))
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
                        && tag.Is(Tag.BitString)
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