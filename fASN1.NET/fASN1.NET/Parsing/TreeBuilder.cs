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
    public static bool TryBuild<TSource>(ref TSource source, ReadOnlySpan<ScannedNode> nodes, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? root, ref ParseFailure failure)
        where TSource : struct, IByteSource
    {
        root = null;
        var parents = new PooledList<PendingParent>(INITIAL_PARENT_CAPACITY, clearOnRemove: true);
        try
        {
            foreach (ref readonly var node in nodes)
            {
                var tag = tagFactory.Create(node.Identifier);
                if (!TryFillContent(ref source, tag, node, ref failure))
                    return false;

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
