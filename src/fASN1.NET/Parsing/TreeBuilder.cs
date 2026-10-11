using System;
using System.Diagnostics.CodeAnalysis;
using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// Second pass of the parser: creates the tags described by the scanned nodes and reads their content.
/// </summary>
/// <remarks>
/// Nodes are in pre-order, so the tree is built front to back: each node becomes the next child of the
/// innermost parent that still expects children. Built-in tags receive their children in an array of
/// exactly the right size.
/// </remarks>
internal static class TreeBuilder
{
    private const int INITIAL_PARENT_CAPACITY = 16;

    /// <summary>Builds the tree; <paramref name="failure"/> is only written when this fails.</summary>
    public static bool TryBuild<TSource>(ref TSource source, ReadOnlySpan<ScannedNode> nodes, ReadOnlySpan<ContentSegment> segments, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? root, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        root = null;
        var parents = new PooledList<PendingParent>(INITIAL_PARENT_CAPACITY, clearOnRemove: true);
        try
        {
            foreach (ref readonly var node in nodes)
            {
                ITag? tag;
                if (node.IsJoinedString)
                {
                    if (!TryBuildJoinedString(ref source, node, segments.Slice(node.FirstSegment, node.SegmentCount), tagFactory, out tag, ref failure))
                        return false;
                }
                else
                {
                    tag = Ber.CreateTag(tagFactory, node.Identifier, node.TagNumber);
                    if (!TryFillContent(ref source, tag, node, ref failure))
                        return false;
                }

                if (parents.Count == 0)
                    root = tag;
                else
                    parents.Last.Add(tag);

                if (node.ContentIsChildren && node.ChildCount > 0)
                    parents.Add(ExpectChildren(tag, node.ChildCount));

                while (parents.Count > 0 && parents.Last.RemainingChildren == 0)
                    parents.RemoveLast();
            }
            return root is not null;
        }
        finally
        {
            parents.Dispose();
        }
    }

    /// <summary>
    /// Creates the tag of a constructed string with its segments joined. A joined OCTET STRING is parsed as a
    /// primitive OCTET STRING with that value would be, so encapsulated content is read the same way.
    /// </summary>
    private static bool TryBuildJoinedString<TSource>(ref TSource source, in ScannedNode node, ReadOnlySpan<ContentSegment> segments,
                                                      ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        tag = null;
        var isOctetString = node.Identifier == (int)Tag.OctetString;
        // For an OCTET STRING, room for the identifier and the longest length in front of the content.
        var offset = isOctetString ? 6 : 0;
        var buffer = new byte[offset + node.ContentLength];
        if (node.Identifier == (int)Tag.BitString)
            buffer[offset++] = (byte)node.UnusedBits;
        foreach (var segment in segments)
        {
            if (!TryReadInto(ref source, buffer, offset, segment.Start, segment.Length))
            {
                failure = new ParseFailure(ParseFailureKind.ContentReadIncomplete, segment.Start, identifier: node.Identifier, tagNumber: node.TagNumber);
                return false;
            }
            offset += (int)segment.Length;
        }

        if (!isOctetString)
        {
            tag = Ber.CreateTag(tagFactory, node.Identifier, node.TagNumber);
            tag.Content = buffer;
            return true;
        }

        // Write 04, the length and nothing else before the content, and parse that as a document of its own.
        var length = (int)node.ContentLength;
        var lengthOctets = length < 0x80 ? 0 : length > 0xFFFFFF ? 4 : length > 0xFFFF ? 3 : length > 0xFF ? 2 : 1;
        var start = 6 - 2 - lengthOctets;
        buffer[start] = (byte)Tag.OctetString;
        buffer[start + 1] = (byte)(lengthOctets == 0 ? length : Ber.LONG_FORM_LENGTH_FLAG | lengthOctets);
        for (int i = lengthOctets; i > 0; i--, length >>= 8)
            buffer[start + 1 + i] = (byte)length;
        var encoded = new ByteArraySource(buffer) { Position = start };
        return Asn1Parser.TryParse(ref encoded, tagFactory, out tag, out _);
    }

    /// <summary>Reads <paramref name="length"/> bytes at <paramref name="start"/> into <paramref name="destination"/>.</summary>
    private static bool TryReadInto<TSource>(ref TSource source, byte[] destination, int offset, long start, long length)
        where TSource : struct, IByteSource
    {
        source.Position = start;
        var end = offset + (int)length;
        while (offset < end)
        {
            var count = source.Read(destination, offset, end - offset);
            if (count == 0)
                return false;
            offset += count;
        }
        return true;
    }

    /// <summary>Gives a leaf its content bytes; <paramref name="failure"/> is only written when this fails.</summary>
    private static bool TryFillContent<TSource>(ref TSource source, ITag tag, in ScannedNode node, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        if (node.ContentIsChildren)
        {
            // A definite-length element read as zero children gets empty content, as an empty leaf would.
            if (node.ChildCount == 0 && !node.IsIndefiniteLength && TagBase.GetChildCount(tag) == 0)
                tag.Content = [];
            return true;
        }

        if (node.IsRolledBackProbe && TagBase.GetChildCount(tag) > 0)
            tag.Children.Clear();
        if (TagBase.GetChildCount(tag) > 0)
            return true; // children the factory gave the tag take the place of content

        var content = ReadContent(ref source, tag, node.ContentStart, node.ContentLength, ref failure);
        if (content is null)
            return false;
        tag.Content = content;
        return true;
    }

    private static PendingParent ExpectChildren(ITag tag, int childCount)
    {
        if (tag is TagBase builtIn && builtIn.ChildCount == 0 && builtIn.Content.Length == 0)
            return new PendingParent { ChildArray = builtIn.InitializeChildArray(childCount), RemainingChildren = childCount };

        // A custom tag, or one the factory created with children already: append through its own list.
        return new PendingParent { ChildList = tag.Children, RemainingChildren = childCount };
    }

    /// <summary>
    /// Reads a leaf's content, or returns <see langword="null"/> and sets <paramref name="failure"/> if the
    /// input ends early. Streams may return fewer bytes than requested, so it reads until complete.
    /// </summary>
    private static byte[]? ReadContent<TSource>(ref TSource source, ITag tag, long start, long length, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        if (length == 0)
            return [];

        source.Position = start;
        var content = new byte[length];
        var read = 0;
        while (read < content.Length)
        {
            var count = source.Read(content, read, content.Length - read);
            if (count == 0)
            {
                failure = new ParseFailure(ParseFailureKind.ContentReadIncomplete, start, tagName: tag.TagName);
                return null;
            }
            read += count;
        }
        return content;
    }
}
