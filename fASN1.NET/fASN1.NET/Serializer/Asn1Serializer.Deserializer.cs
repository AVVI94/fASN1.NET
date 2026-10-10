using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET;

/// <summary>
/// ASN.1 serializer.
/// </summary>
public static partial class Asn1Serializer
{
    #region Deserialize

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified byte array.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize(byte[] data, [NotNullWhen(true)] out ITag? tag)
        => TryDeserialize(data, out tag, out _);

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified byte array using the specified tag factory.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize(byte[] data, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag)
        => TryDeserialize(data, tagFactory, out tag, out _);

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing the ASN.1 data.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize(Stream stream, [NotNullWhen(true)] out ITag? tag)
        => TryDeserialize(stream, out tag, out _);

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified stream using the specified tag factory.
    /// </summary>
    /// <param name="stream">The stream containing the ASN.1 data.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize(Stream stream, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag)
        => TryDeserialize(stream, tagFactory, out tag, out _);

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified byte array.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <param name="error">When this method returns <see langword="false"/>, contains the error message; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize(byte[] data, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        return TryDeserialize(data, DefaultFactory, out tag, out error);
    }

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified byte array using the specified tag factory.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <param name="error">When this method returns <see langword="false"/>, contains the error message; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize(byte[] data, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        using var ms = new MemoryStream(data);
        return TryDeserialize(ms, tagFactory, out tag, out error);
    }

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing the ASN.1 data.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <param name="error">When this method returns <see langword="false"/>, contains the error message; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize(Stream stream, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        return TryDeserialize(stream, DefaultFactory, out tag, out error);
    }

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified stream using the specified tag factory.
    /// </summary>
    /// <param name="stream">The stream containing the ASN.1 data.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <param name="error">When this method returns <see langword="false"/>, contains the error message; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize(Stream stream, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        try
        {
            error = null;
            if (stream is null or { Length: 0 })
            {
                error = "Stream is null or empty.";
                tag = null;
                return false;
            }
            tag = DeserializeInternal(stream, ref error, tagFactory);
            if (tag is not null)
                error = null;
            return tag is not null;
        }
        catch (Exception ex) //just to be safe
        {
            tag = null;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Deserializes the ASN.1 data from the specified byte array.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>    
    /// <returns>The deserialized ASN.1 tag.</returns>
    public static ITag Deserialize(byte[] data)
    {
        return Deserialize(data, DefaultFactory);
    }

    /// <summary>
    /// Deserializes the ASN.1 data from the specified byte array using the specified tag factory.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <returns>The deserialized ASN.1 tag.</returns>
    public static ITag Deserialize(byte[] data, ITagFactory tagFactory)
    {
        using var ms = new MemoryStream(data);
        return Deserialize(ms, tagFactory);
    }

    /// <summary>
    /// Deserializes the ASN.1 data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing the ASN.1 data.<br/>
    /// This stream must have the <see cref="Stream.CanSeek"/> property set to <see langword="true"/>.
    /// </param>    
    /// <returns>The deserialized ASN.1 tag.</returns>
    public static ITag Deserialize(Stream stream)
    {
        return Deserialize(stream, DefaultFactory);
    }

    /// <summary>
    /// Deserializes the ASN.1 data from the specified stream using the specified tag factory.
    /// </summary>
    /// <param name="stream">The stream containing the ASN.1 data.<br/>
    /// This stream must have the <see cref="Stream.CanSeek"/> property set to <see langword="true"/>. 
    /// </param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <returns>The deserialized ASN.1 tag.</returns>
    public static ITag Deserialize(Stream stream, ITagFactory tagFactory)
    {
        string? error = null;
        if (stream is null or { Length: 0 })
            throw new ArgumentException("Stream is null or empty.", nameof(stream));
        return DeserializeInternal(stream, ref error, tagFactory) ?? throw new Asn1DeserializationException(error ?? "Unknown deserialization error");
    }

    // Parsing runs in two passes over the input:
    //  1. Scan: read every identifier and length, validate the structure and settle each OCTET STRING
    //     encapsulation probe, without creating any tag. The final tree is recorded as a flat pre-order
    //     list of NodeRecords. A failed probe just truncates that list back to the probe itself.
    //  2. Materialize: create exactly the recorded tags, each container's children list at its final
    //     size, and read only the content that ends up in the tree.
    // So nothing is allocated for structure that is later discarded, and invalid input allocates nothing
    // but the error message.

    /// <summary>Stateless, so one instance serves every call that does not pass its own factory.</summary>
    private static readonly DefaultTagFactory DefaultFactory = new();

    /// <summary>
    /// Per-thread working buffers, reused across calls. Taken out of the slot while in use, so a nested
    /// parse (for example from a custom factory) gets its own.
    /// </summary>
    [ThreadStatic]
    private static ParseState? t_cachedState;

    private static ITag? DeserializeInternal(Stream stream, ref string? error, ITagFactory tagFactory)
    {
        var state = t_cachedState ?? new ParseState();
        t_cachedState = null;
        var failure = default(ParseError);
        try
        {
            var traits = state.TraitsFor(tagFactory);
            if (!Scan(stream, ref failure, traits, state))
            {
                error = failure.Format();
                return null;
            }
            var end = stream.Position;
            var root = Materialize(stream, ref failure, tagFactory, state);
            if (root is null)
            {
                error = failure.Format();
                return null;
            }
            stream.Position = end;
            return root;
        }
        finally
        {
            if (state.Reset())
                t_cachedState = state;
        }
    }

    #region Errors

    private enum ErrorKind
    {
        None,
        UnexpectedEnd,
        MissingEndOfContents,
        MultiByteTag,
        InvalidEndOfContents,
        ContainerPastEnd,
        ContainerSizeMismatch,
        IndefinitePrimitive,
        ContentPastEnd,
        ContentReadShort,
        LengthTruncated,
        TooManyLengthOctets,
        LengthTooLarge,
    }

    /// <summary>
    /// A parse failure, kept as data and formatted only if the whole parse fails. Failed encapsulation
    /// probes record failures too, and most parses that see them still succeed.
    /// </summary>
    private readonly struct ParseError(ErrorKind kind, long offset, long value = 0, string? name = null)
    {
        public string? Format() => kind switch
        {
            ErrorKind.None => null,
            ErrorKind.UnexpectedEnd => $"Unexpected end of data at offset {offset}",
            ErrorKind.MissingEndOfContents => $"Indefinite-length container at offset {offset} has no end-of-contents marker",
            ErrorKind.MultiByteTag => $"Element at offset {offset} uses a multi-byte tag number, which is not supported",
            ErrorKind.InvalidEndOfContents => $"Invalid end-of-contents marker at offset {offset}",
            ErrorKind.ContainerPastEnd => $"Container at offset {offset} has a length of {value}, which is past the end of the stream",
            ErrorKind.ContainerSizeMismatch => $"Content size is not correct for container at offset {offset}",
            ErrorKind.IndefinitePrimitive => "Cannot skip over an invalid tag with indefinite length",
            ErrorKind.ContentPastEnd => $"Content size is not correct for tag {name} at offset {offset}",
            ErrorKind.ContentReadShort => $"Unexpected end of data in the content of tag {name} at offset {offset}",
            ErrorKind.LengthTruncated => $"Unexpected end of data in the length of the element at offset {offset}",
            ErrorKind.TooManyLengthOctets => $"Length of the element at offset {offset} uses {value} octets; at most 6 are supported",
            ErrorKind.LengthTooLarge => $"Length of the element at offset {offset} is too large",
            _ => $"Invalid data at offset {offset}",
        };
    }

    #endregion

    #region Tag traits

    /// <summary>What the scan needs to know about the tag a factory creates for an identifier.</summary>
    private readonly struct TagTraits(bool constructed, bool probe, string name)
    {
        /// <summary>The content is read as nested elements.</summary>
        public bool Constructed { get; } = constructed;

        /// <summary>A primitive universal OCTET STRING: its content is tried as nested elements.</summary>
        public bool Probe { get; } = probe;

        /// <summary>Tag name, used in error messages.</summary>
        public string Name { get; } = name;

        public bool IsKnown => Name is not null;

        public static TagTraits Of(ITag freshTag, int identifier)
        {
            var constructed = freshTag.IsConstructed || (identifier & 0x20) != 0;
            // A BIT STRING is always a leaf: signatures and keys would otherwise be expanded by accident.
            var probe = !constructed && freshTag.IsUniversal && freshTag.TagNumber == (int)Tag.OctetString;
            return new TagTraits(constructed, probe, freshTag.TagName);
        }
    }

    /// <summary>
    /// Traits per identifier for one factory. Each identifier is asked of the factory once, by creating a
    /// tag; the decisions depend only on a freshly created tag, as they always have.
    /// </summary>
    private sealed class TraitsTable(ITagFactory factory)
    {
        private readonly TagTraits[] _traits = new TagTraits[256];

        public ITagFactory Factory { get; } = factory;

        public TagTraits this[int identifier]
        {
            get
            {
                ref var traits = ref _traits[identifier];
                if (!traits.IsKnown)
                    traits = TagTraits.Of(Factory.Create(identifier), identifier);
                return traits;
            }
        }

        /// <summary>The default factory's table, filled once for every identifier so it can be shared by all threads.</summary>
        public static TraitsTable CreateComplete(ITagFactory factory)
        {
            var table = new TraitsTable(factory);
            for (int i = 0; i < 256; i++)
                _ = table[i];
            return table;
        }
    }

    private static readonly TraitsTable DefaultTraits = TraitsTable.CreateComplete(DefaultFactory);

    #endregion

    #region Working state

    /// <summary>One node of the final tree, in pre-order, as found by the scan.</summary>
    private struct NodeRecord
    {
        public int Identifier;

        /// <summary>Stream offset of the first content byte.</summary>
        public long Start;

        /// <summary>Content length of a leaf.</summary>
        public long Length;

        /// <summary>Children were read; the next <see cref="ChildCount"/> subtrees in pre-order are them.</summary>
        public bool IsContainer;

        public bool IsIndefinite;

        public int ChildCount;

        /// <summary>A probe that did not hold nested elements: a leaf whose content is the raw bytes.</summary>
        public bool RolledBack;
    }

    /// <summary>What a frame's children are being read for.</summary>
    private enum FrameKind
    {
        /// <summary>A constructed element; a failure in its children fails the element.</summary>
        Container,

        /// <summary>A primitive OCTET STRING whose content is tried as nested elements; a failure rolls it back to a leaf.</summary>
        Probe,
    }

    /// <summary>An element whose children are being scanned.</summary>
    private struct ContainerFrame
    {
        public FrameKind Kind;

        /// <summary>This element's entry in the record list.</summary>
        public int Record;

        /// <summary>Stream offset of the first content byte.</summary>
        public long Start;

        /// <summary>Declared content length; <see langword="null"/> for indefinite length.</summary>
        public long? Length;

        public readonly bool IsIndefinite => Length is null;

        /// <summary>Offset where definite-length children must end.</summary>
        public readonly long End => Start + Length!.Value;

        /// <summary>Number of direct children scanned so far.</summary>
        public int ChildCount;

        /// <summary>A probe saw an EOC among its direct children, so its content is not a run of elements.</summary>
        public bool ContainsEoc;

        /// <summary>Tag name, for the error message if a rolled-back probe's content is missing.</summary>
        public string Name;
    }

    /// <summary>Entry of the materialize pass's parent stack.</summary>
    private struct OpenParent
    {
        public IList<ITag> Children;
        public int Remaining;
    }

    /// <summary>
    /// Buffers of both passes. Array-backed, handing out entries by reference so structs are not copied.
    /// </summary>
    private sealed class ParseState
    {
        public ContainerFrame[] Frames = new ContainerFrame[16];
        public int FrameCount;
        public NodeRecord[] Records = new NodeRecord[64];
        public int RecordCount;
        public OpenParent[] Parents = new OpenParent[16];
        private TraitsTable? _customTraits;

        public ref ContainerFrame Top => ref Frames[FrameCount - 1];

        public void Push(in ContainerFrame frame)
        {
            if (FrameCount == Frames.Length)
                Array.Resize(ref Frames, Frames.Length * 2);
            Frames[FrameCount++] = frame;
        }

        public ContainerFrame Pop() => Frames[--FrameCount];

        public int AddRecord(in NodeRecord record)
        {
            if (RecordCount == Records.Length)
                Array.Resize(ref Records, Records.Length * 2);
            Records[RecordCount] = record;
            return RecordCount++;
        }

        public TraitsTable TraitsFor(ITagFactory factory)
        {
            if (ReferenceEquals(factory, DefaultFactory))
                return DefaultTraits;
            if (_customTraits is null || !ReferenceEquals(_customTraits.Factory, factory))
                _customTraits = new TraitsTable(factory);
            return _customTraits;
        }

        /// <summary>
        /// Clears the buffers after a parse, including one that ended in an exception, so they hold no tags.
        /// Returns <see langword="false"/> when they grew too large to be worth keeping for the next parse.
        /// </summary>
        public bool Reset()
        {
            FrameCount = 0;
            RecordCount = 0;
            Array.Clear(Parents, 0, Parents.Length);
            return Frames.Length <= 256 && Records.Length <= 16384 && Parents.Length <= 256;
        }
    }

    #endregion

    #region Pass 1: scan

    /// <summary>
    /// What <see cref="ScanElement"/> did with the next element.
    /// </summary>
    private enum Step
    {
        /// <summary>The element was scanned completely; see the success flag.</summary>
        Completed,

        /// <summary>A frame was pushed; its children come next.</summary>
        Opened,

        /// <summary>The end-of-contents marker of the innermost indefinite-length frame was read.</summary>
        EndOfContents,
    }

    /// <summary>
    /// Scans one element and all of its descendants, recording the final tree. Every element whose
    /// children are being read is a frame on an explicit stack, so nesting depth does not consume call stack space.
    /// </summary>
    private static bool Scan(Stream stream, ref ParseError error, TraitsTable traits, ParseState state)
    {
        var needElement = true;
        var ok = false;
        while (true)
        {
            var step = needElement ? ScanElement(stream, ref error, traits, state, out ok) : Step.Completed;
            if (step == Step.EndOfContents)
            {
                ok = EndFrame(stream, ref error, state, state.Pop(), success: true);
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
                    ok = EndFrame(stream, ref error, state, state.Pop(), success: false);
                    needElement = false;
                    continue;
                }
                state.Top.ChildCount++;
            }

            ref var top = ref state.Top;
            if (top.IsIndefinite || stream.Position < top.End)
            {
                needElement = true;
                continue;
            }

            var done = state.Pop();
            needElement = false;
            var exact = stream.Position == done.End;
            if (!exact)
            {
                error = new ParseError(ErrorKind.ContainerSizeMismatch, done.Start);
            }
            ok = EndFrame(stream, ref error, state, done, exact);
        }
    }

    /// <summary>
    /// Reads the identifier and length of the next element and records it. A leaf is scanned completely;
    /// an element whose children have to be read is pushed as a frame.
    /// </summary>
    private static Step ScanElement(Stream stream, ref ParseError error, TraitsTable traits, ParseState state, out bool ok)
    {
        ok = false;
        var elementStart = stream.Position;
        var tagNumber = stream.ReadByte();
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
        long? length = DecodeLength(stream, elementStart, ref error, out var valid);
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
        var start = stream.Position;
        var record = state.AddRecord(new NodeRecord { Identifier = tagNumber, Start = start, Length = length ?? 0, IsIndefinite = length is null });

        if (tagTraits.Constructed || (length is not null && tagTraits.Probe))
        {
            var frame = new ContainerFrame
            {
                Kind = tagTraits.Constructed ? FrameKind.Container : FrameKind.Probe,
                Record = record,
                Start = start,
                Length = length,
                Name = tagTraits.Name,
            };
            if (!frame.IsIndefinite && frame.End > stream.Length)
            {
                error = new ParseError(ErrorKind.ContainerPastEnd, frame.Start, frame.Length!.Value);
                ok = EndFrame(stream, ref error, state, frame, success: false);
                return Step.Completed;
            }
            state.Push(frame);
            return Step.Opened;
        }

        ok = SkipContent(stream, ref error, tagTraits.Name, start, length);
        return Step.Completed;
    }

    /// <summary>
    /// Completes an element once its children have been scanned, successfully or not.
    /// </summary>
    private static bool EndFrame(Stream stream, ref ParseError error, ParseState state, in ContainerFrame frame, bool success)
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
        stream.Position = frame.Start;
        return SkipContent(stream, ref error, frame.Name, frame.Start, frame.Length);
    }

    /// <summary>
    /// Checks that a leaf's content is present and moves past it; it is read in the materialize pass.
    /// </summary>
    private static bool SkipContent(Stream stream, ref ParseError error, string tagName, long start, long? length)
    {
        if (length is null)
        {
            error = new ParseError(ErrorKind.IndefinitePrimitive, start);
            return false;
        }
        if (length.Value > stream.Length - stream.Position)
        {
            error = new ParseError(ErrorKind.ContentPastEnd, start, name: tagName);
            return false;
        }
        stream.Position += length.Value;
        return true;
    }

    #endregion

    #region Pass 2: materialize

    /// <summary>
    /// Creates the recorded tree. Records are in pre-order, so each container's children are the
    /// subtrees that follow it.
    /// </summary>
    private static ITag? Materialize(Stream stream, ref ParseError error, ITagFactory tagFactory, ParseState state)
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
                    var content = ReadContent(stream, ref error, tag, record.Start, record.Length);
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
                parent.Children.Add(tag);
                parent.Remaining--;
            }

            if (record.IsContainer && record.ChildCount > 0)
            {
                IList<ITag> children;
                if (tag is TagBase builtIn && builtIn.ChildCount == 0)
                {
                    var list = new List<ITag>(record.ChildCount);
                    builtIn.SetParsedChildren(list);
                    children = list;
                }
                else
                {
                    // A custom tag, or one the factory created with children already: append through its own list.
                    children = tag.Children;
                }
                if (depth == state.Parents.Length)
                    Array.Resize(ref state.Parents, state.Parents.Length * 2);
                state.Parents[depth++] = new OpenParent { Children = children, Remaining = record.ChildCount };
            }

            while (depth > 0 && state.Parents[depth - 1].Remaining == 0)
            {
                state.Parents[--depth] = default;
            }
        }
        return root;
    }

    /// <summary>
    /// Reads a leaf's content. Streams may return fewer bytes than requested, so it reads until complete.
    /// </summary>
    private static byte[]? ReadContent(Stream stream, ref ParseError error, ITag tag, long start, long length)
    {
        if (length == 0)
            return [];
        stream.Position = start;
        var content = new byte[length];
        var read = 0;
        while (read < content.Length)
        {
            var n = stream.Read(content, read, content.Length - read);
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


    /// <summary>
    /// Reads the length octets of the element whose identifier is at <paramref name="elementStart"/>.
    /// Returns <see langword="null"/> with <paramref name="valid"/> set for indefinite length; on failure
    /// <paramref name="valid"/> is <see langword="false"/> and <paramref name="error"/> says why.
    /// </summary>
    static long? DecodeLength(Stream data, long elementStart, ref ParseError error, out bool valid)
    {
        valid = false;
        var first = data.ReadByte();
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
            var b = data.ReadByte();
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
}
