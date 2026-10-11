using System;
using System.Diagnostics.CodeAnalysis;
using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// Second pass of the parser: creates the tags described by the scanned nodes and reads the contents octets of the
/// primitive encodings among them.
/// </summary>
/// <remarks>
/// Nodes are in pre-order, so the tree is built front to back: each node becomes the next child of the
/// innermost parent that still expects children. Built-in tags receive their children in an array of
/// exactly the right size.
/// </remarks>
internal static class TreeBuilder
{
    private const int INITIAL_PARENT_CAPACITY = 16;

    /// <summary>The identifier octet of OCTET STRING: universal 4, tag number below 31, a single octet.</summary>
    private const int IDENTIFIER_OCTET_COUNT = 1;

    /// <summary>Room for the identifier and length octets of a primitive OCTET STRING of any <see cref="int"/> length.</summary>
    private const int IDENTIFIER_AND_LENGTH_ROOM = IDENTIFIER_OCTET_COUNT + Ber.MAX_SHORTEST_LENGTH_OCTETS;

    /// <summary>Builds the tree; <paramref name="failure"/> is only written when this fails.</summary>
    public static bool TryBuild<TSource>(ref TSource source, ReadOnlySpan<ScannedNode> nodes, ReadOnlySpan<ContentsFragment> fragments, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? root, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        root = null;
        var parents = new PooledList<PendingParent>(INITIAL_PARENT_CAPACITY, clearOnRemove: true);
        try
        {
            foreach (ref readonly var node in nodes)
            {
                ITag? tag;
                if (node.IsConstructedString)
                {
                    if (!TryBuildConstructedString(ref source, node, fragments.Slice(node.FirstFragment, node.FragmentCount), tagFactory, out tag, ref failure))
                    {
                        return false;
                    }
                }
                else
                {
                    tag = CreateEmptyTag(tagFactory, node.IdentifierOctet, node.TagNumber);
                    if (!TryFillContents(ref source, tag, node, ref failure))
                    {
                        return false;
                    }
                }

                if (parents.Count == 0)
                {
                    root = tag;
                }
                else
                {
                    parents.Last.Add(tag);
                }

                if (node.ContentsAreChildren && node.ChildCount > 0)
                {
                    parents.Add(ExpectChildren(tag, node.ChildCount));
                }

                while (parents.Count > 0 && parents.Last.RemainingChildren == 0)
                {
                    parents.RemoveLast();
                }
            }
            return root is not null;
        }
        finally
        {
            parents.Dispose();
        }
    }

    /// <summary>
    /// Creates the tag of a constructed string with the contents of its fragments joined. A constructed OCTET STRING is
    /// then parsed as the primitive OCTET STRING with that value would be, so encapsulated encodings are read the same
    /// way.
    /// </summary>
    private static bool TryBuildConstructedString<TSource>(ref TSource source, in ScannedNode node, ReadOnlySpan<ContentsFragment> fragments,
                                                      ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        tag = null;
        var isOctetString = node.IdentifierOctet == (int)Tag.OctetString;
        // For an OCTET STRING, room for its identifier octet and the longest length octets in front of the contents.
        var offset = isOctetString ? IDENTIFIER_AND_LENGTH_ROOM : 0;
        var buffer = new byte[offset + node.ContentsLength];
        if (node.IdentifierOctet == (int)Tag.BitString)
        {
            buffer[offset++] = (byte)node.UnusedBits;
        }

        foreach (var fragment in fragments)
        {
            if (!TryReadInto(ref source, buffer, offset, fragment.Start, fragment.Length))
            {
                failure = new ParseFailure(ParseFailureKind.ContentsReadIncomplete, fragment.Start, identifierOctet: node.IdentifierOctet, tagNumber: node.TagNumber);
                return false;
            }
            offset += (int)fragment.Length;
        }

        if (!isOctetString)
        {
            tag = CreateEmptyTag(tagFactory, node.IdentifierOctet, node.TagNumber);
            tag.Content = buffer;
            return true;
        }

        // Write the identifier and length octets of the primitive encoding in front of the contents, and parse that.
        var length = (int)node.ContentsLength;
        var start = IDENTIFIER_AND_LENGTH_ROOM - IDENTIFIER_OCTET_COUNT - Ber.LengthOctetCount(length);
        buffer[start] = (byte)Tag.OctetString;
        Ber.WriteLengthOctets(length, buffer, start + IDENTIFIER_OCTET_COUNT);
        var encoded = new ByteArraySource(buffer) { Position = start };
        return Asn1Parser.TryParse(ref encoded, tagFactory, out tag, out _);
    }

    /// <summary>Reads <paramref name="length"/> octets at <paramref name="start"/> into <paramref name="destination"/>.</summary>
    private static bool TryReadInto<TSource>(ref TSource source, byte[] destination, int offset, long start, long length)
        where TSource : struct, IByteSource
    {
        source.Position = start;
        var end = offset + (int)length;
        while (offset < end)
        {
            var count = source.Read(destination, offset, end - offset);
            if (count == 0)
            {
                return false;
            }

            offset += count;
        }
        return true;
    }

    /// <summary>Gives a primitive encoding's tag its contents octets; <paramref name="failure"/> is only written when this fails.</summary>
    private static bool TryFillContents<TSource>(ref TSource source, ITag tag, in ScannedNode node, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        if (node.ContentsAreChildren)
        {
            return true;
        }

        var contents = ReadContents(ref source, tag, node.ContentsStart, node.ContentsLength, ref failure);
        if (contents is null)
        {
            return false;
        }

        tag.Content = contents;
        return true;
    }

    /// <summary>
    /// Creates a node's tag through the factory and removes any contents or children the factory gave it, so the tree
    /// holds exactly what the encoding holds.
    /// </summary>
    private static ITag CreateEmptyTag(ITagFactory tagFactory, int identifierOctet, int tagNumber)
    {
        var tag = Ber.CreateTag(tagFactory, identifierOctet, tagNumber);
        if (TagBase.GetChildCount(tag) > 0)
        {
            tag.Children.Clear();
        }

        if (tag.Content is not { Length: 0 })
        {
            tag.Content = [];
        }

        return tag;
    }

    private static PendingParent ExpectChildren(ITag tag, int childCount)
    {
        if (tag is TagBase builtIn)
        {
            return new PendingParent { ChildArray = builtIn.InitializeChildArray(childCount), RemainingChildren = childCount };
        }

        // A custom tag: append through its own list.
        return new PendingParent { ChildList = tag.Children, RemainingChildren = childCount };
    }

    /// <summary>
    /// Reads the contents octets of a primitive encoding, or returns <see langword="null"/> and sets
    /// <paramref name="failure"/> if the input ends early. Streams may return fewer bytes than requested, so it reads
    /// until complete.
    /// </summary>
    private static byte[]? ReadContents<TSource>(ref TSource source, ITag tag, long start, long length, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        if (length == 0)
        {
            return [];
        }

        source.Position = start;
        var contents = new byte[length];
        var read = 0;
        while (read < contents.Length)
        {
            var count = source.Read(contents, read, contents.Length - read);
            if (count == 0)
            {
                failure = new ParseFailure(ParseFailureKind.ContentsReadIncomplete, start, tagName: tag.TagName);
                return null;
            }
            read += count;
        }
        return contents;
    }
}
