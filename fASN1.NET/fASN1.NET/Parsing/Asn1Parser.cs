using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// The BER/DER parser behind <see cref="Asn1Serializer.Deserialize(byte[])"/> and its overloads.
/// </summary>
/// <remarks>
/// Parsing runs in two passes over the input:
/// <list type="number">
/// <item>Scan: read every identifier and length, validate the structure and settle each OCTET STRING
/// encapsulation probe, without creating any tag. The final tree is recorded as a flat pre-order list of
/// <see cref="NodeRecord"/>s. A failed probe just truncates that list back to the probe itself.</item>
/// <item>Materialize: create exactly the recorded tags, each container's children at their final size,
/// and read only the content that ends up in the tree.</item>
/// </list>
/// Nothing is allocated for structure that is later discarded, and invalid input allocates nothing but
/// the error message. The working buffers are rented from the shared array pools for the duration of one
/// call; the parser keeps no state between calls. Every element whose children are being read is a frame
/// on an explicit stack, so nesting depth does not consume call stack space.
/// </remarks>
internal static class Asn1Parser
{
    /// <summary>
    /// Parses the element at the source's current position. On success the source is left just past it.
    /// </summary>
    /// <returns>The parsed tag, or <see langword="null"/> with <paramref name="error"/> describing the failure.</returns>
    public static ITag? Parse<TSource>(ref TSource source, ITagFactory tagFactory, out string? error)
        where TSource : struct, IByteSource
    {
        error = null;
        var buffers = ParseBuffers.Rent();
        var traits = TraitsLookup.For(tagFactory);
        var failure = default(ParseError);
        try
        {
            if (!Scan(ref source, ref failure, traits, ref buffers))
            {
                error = failure.Format();
                return null;
            }
            var end = source.Position;
            var root = Materialize(ref source, ref failure, tagFactory, ref buffers);
            if (root is null)
            {
                error = failure.Format();
                return null;
            }
            source.Position = end;
            return root;
        }
        finally
        {
            traits.Release();
            buffers.Return();
        }
    }

    #region Pass 1: scan

    /// <summary>What <see cref="ScanElement"/> did with the next element.</summary>
    private enum Step
    {
        /// <summary>The element was scanned completely; see the success flag.</summary>
        Completed,

        /// <summary>A frame was pushed; its children come next.</summary>
        Opened,

        /// <summary>The end-of-contents marker of the innermost indefinite-length frame was read.</summary>
        EndOfContents,
    }

    /// <summary>Scans one element and all of its descendants, recording the final tree.</summary>
    private static bool Scan<TSource>(ref TSource source, ref ParseError error, in TraitsLookup traits, ref ParseBuffers state)
        where TSource : struct, IByteSource
    {
        var needElement = true;
        var ok = false;
        while (true)
        {
            var step = needElement ? ScanElement(ref source, ref error, traits, ref state, out ok) : Step.Completed;
            if (step == Step.EndOfContents)
            {
                ok = EndFrame(ref source, ref error, traits, ref state, state.Pop(), success: true);
                needElement = false;
                continue;
            }
            if (step == Step.Completed)
            {
                if (state.FrameCount == 0)
                    return ok;
                if (!ok)
                {
                    // A failed child ends the frame unsuccessfully.
                    ok = EndFrame(ref source, ref error, traits, ref state, state.Pop(), success: false);
                    needElement = false;
                    continue;
                }
                state.Top.ChildCount++;
            }

            ref var top = ref state.Top;
            if (top.IsIndefinite || source.Position < top.End)
            {
                needElement = true;
                continue;
            }

            var done = state.Pop();
            needElement = false;
            var exact = source.Position == done.End;
            if (!exact)
            {
                error = new ParseError(ErrorKind.ContainerSizeMismatch, done.Start);
            }
            ok = EndFrame(ref source, ref error, traits, ref state, done, exact);
        }
    }

    /// <summary>
    /// Reads the identifier and length of the next element and records it. A leaf is scanned completely;
    /// an element whose children have to be read is pushed as a frame.
    /// </summary>
    private static Step ScanElement<TSource>(ref TSource source, ref ParseError error, in TraitsLookup traits, ref ParseBuffers state, out bool ok)
        where TSource : struct, IByteSource
    {
        ok = false;
        var elementStart = source.Position;
        var tagNumber = source.ReadByte();
        var inIndefinite = state.FrameCount > 0 && state.Top.IsIndefinite;
        if (tagNumber == -1)
        {
            error = inIndefinite
                ? new ParseError(ErrorKind.MissingEndOfContents, state.Top.Start)
                : new ParseError(ErrorKind.UnexpectedEnd, elementStart);
            return Step.Completed;
        }
        if ((tagNumber & 0x1F) == 0x1F)
        {
            // The tag number continues in the following bytes; reading them as the length would misplace every later element.
            error = new ParseError(ErrorKind.MultiByteTag, elementStart);
            return Step.Completed;
        }
        long? length = DecodeLength(ref source, elementStart, ref error, out var valid);
        if (!valid)
        {
            return Step.Completed;
        }

        if (tagNumber == 0)
        {
            if (inIndefinite)
            {
                if (length == 0)
                    return Step.EndOfContents;
                error = new ParseError(ErrorKind.InvalidEndOfContents, elementStart);
                return Step.Completed;
            }
            if (state.FrameCount > 0 && state.Top.Kind == FrameKind.Probe)
            {
                // EOC cannot be content, so this OCTET STRING does not wrap nested elements.
                state.Top.ContainsEoc = true;
            }
        }

        var tagTraits = traits[tagNumber];
        var start = source.Position;
        var record = state.AddRecord(new NodeRecord { Identifier = tagNumber, Start = start, Length = length ?? 0, IsIndefinite = length is null });

        if (tagTraits.Constructed || (length is not null && tagTraits.Probe))
        {
            var frame = new ContainerFrame
            {
                Kind = tagTraits.Constructed ? FrameKind.Container : FrameKind.Probe,
                Record = record,
                Start = start,
                Length = length,
            };
            if (!frame.IsIndefinite && frame.End > source.Length)
            {
                error = new ParseError(ErrorKind.ContainerPastEnd, frame.Start, frame.Length!.Value);
                ok = EndFrame(ref source, ref error, traits, ref state, frame, success: false);
                return Step.Completed;
            }
            state.Push(frame);
            return Step.Opened;
        }

        ok = SkipContent(ref source, ref error, tagTraits.Name, start, length);
        return Step.Completed;
    }

    /// <summary>Completes an element once its children have been scanned, successfully or not.</summary>
    private static bool EndFrame<TSource>(ref TSource source, ref ParseError error, in TraitsLookup traits, ref ParseBuffers state, in ContainerFrame frame, bool success)
        where TSource : struct, IByteSource
    {
        ref var record = ref state.Records[frame.Record];
        if (frame.Kind == FrameKind.Container)
        {
            if (!success)
                return false;
            record.IsContainer = true;
            record.ChildCount = frame.ChildCount;
            return true;
        }

        if (success && !frame.ContainsEoc)
        {
            record.IsContainer = true;
            record.ChildCount = frame.ChildCount;
            return true;
        }

        // Not a complete run of nested elements: forget the children and keep the content as raw bytes.
        state.RecordCount = frame.Record + 1;
        record.RolledBack = true;
        source.Position = frame.Start;
        return SkipContent(ref source, ref error, traits[record.Identifier].Name, frame.Start, frame.Length);
    }

    /// <summary>Checks that a leaf's content is present and moves past it; it is read in the materialize pass.</summary>
    private static bool SkipContent<TSource>(ref TSource source, ref ParseError error, string tagName, long start, long? length)
        where TSource : struct, IByteSource
    {
        if (length is null)
        {
            error = new ParseError(ErrorKind.IndefinitePrimitive, start);
            return false;
        }
        if (length.Value > source.Length - source.Position)
        {
            error = new ParseError(ErrorKind.ContentPastEnd, start, name: tagName);
            return false;
        }
        source.Position += length.Value;
        return true;
    }

    /// <summary>
    /// Reads the length octets of the element whose identifier is at <paramref name="elementStart"/>.
    /// Returns <see langword="null"/> with <paramref name="valid"/> set for indefinite length; on failure
    /// <paramref name="valid"/> is <see langword="false"/> and <paramref name="error"/> says why.
    /// </summary>
    private static long? DecodeLength<TSource>(ref TSource source, long elementStart, ref ParseError error, out bool valid)
        where TSource : struct, IByteSource
    {
        valid = false;
        var first = source.ReadByte();
        if (first == -1)
        {
            error = new ParseError(ErrorKind.LengthTruncated, elementStart);
            return null;
        }
        if (first < 0x80)
        {
            valid = true;
            return first;
        }
        var octets = first & 0x7F;
        if (octets == 0)
        {
            valid = true;
            return null;
        }
        if (octets > 6)
        {
            error = new ParseError(ErrorKind.TooManyLengthOctets, elementStart, octets);
            return null;
        }

        long value = 0;
        for (int i = 0; i < octets; i++)
        {
            var b = source.ReadByte();
            if (b == -1)
            {
                error = new ParseError(ErrorKind.LengthTruncated, elementStart);
                return null;
            }
            value = (value << 8) | (uint)b;
        }

        // Content is read into a byte[], so lengths are limited to what an array can index.
        if (value > int.MaxValue)
        {
            error = new ParseError(ErrorKind.LengthTooLarge, elementStart);
            return null;
        }
        valid = true;
        return value;
    }

    #endregion

    #region Pass 2: materialize

    /// <summary>
    /// Creates the recorded tree. Records are in pre-order, so each container's children are the
    /// subtrees that follow it.
    /// </summary>
    private static ITag? Materialize<TSource>(ref TSource source, ref ParseError error, ITagFactory tagFactory, ref ParseBuffers state)
        where TSource : struct, IByteSource
    {
        ITag? root = null;
        var depth = 0;
        for (int i = 0; i < state.RecordCount; i++)
        {
            ref var record = ref state.Records[i];
            var tag = tagFactory.Create(record.Identifier);

            if (record.IsContainer)
            {
                // A definite-length container without children reads its (empty) content, as a leaf would.
                if (record.ChildCount == 0 && !record.IsIndefinite && TagBase.CountChildren(tag) == 0)
                    tag.Content = [];
            }
            else
            {
                if (record.RolledBack && TagBase.CountChildren(tag) > 0)
                    tag.Children.Clear();
                if (TagBase.CountChildren(tag) == 0)
                {
                    var content = ReadContent(ref source, ref error, tag, record.Start, record.Length);
                    if (content is null)
                        return null;
                    tag.Content = content;
                }
            }

            if (depth == 0)
            {
                root = tag;
            }
            else
            {
                ref var parent = ref state.Parents[depth - 1];
                if (parent.Array is not null)
                    parent.Array[parent.Next++] = tag;
                else
                    parent.List!.Add(tag);
                parent.Remaining--;
            }

            if (record.IsContainer && record.ChildCount > 0)
            {
                var parent = new OpenParent { Remaining = record.ChildCount };
                if (tag is TagBase builtIn && builtIn.ChildCount == 0 && builtIn.Content.Length == 0)
                    parent.Array = builtIn.SetParsedChildren(record.ChildCount);
                else
                    parent.List = tag.Children; // a custom tag, or one the factory created with children already
                state.ParentAt(depth++) = parent;
            }

            while (depth > 0 && state.Parents[depth - 1].Remaining == 0)
            {
                state.Parents[--depth] = default;
            }
        }
        return root;
    }

    /// <summary>Reads a leaf's content. Streams may return fewer bytes than requested, so it reads until complete.</summary>
    private static byte[]? ReadContent<TSource>(ref TSource source, ref ParseError error, ITag tag, long start, long length)
        where TSource : struct, IByteSource
    {
        if (length == 0)
            return [];
        source.Position = start;
        var content = new byte[length];
        var read = 0;
        while (read < content.Length)
        {
            var n = source.Read(content, read, content.Length - read);
            if (n == 0)
            {
                error = new ParseError(ErrorKind.ContentReadShort, start, name: tag.TagName);
                return null;
            }
            read += n;
        }
        return content;
    }

    #endregion
}
