using System;
using System.Buffers;
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
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        var source = new ArrayByteSource(data);
        return TryDeserializeCore(ref source, tagFactory, out tag, out error);
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
            if (stream is null)
            {
                error = "Stream is null or empty.";
                tag = null;
                return false;
            }
            var source = new StreamByteSource(stream);
            return TryDeserializeCore(ref source, tagFactory, out tag, out error);
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
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        var source = new ArrayByteSource(data);
        return DeserializeCore(ref source, tagFactory, nameof(data));
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
        if (stream is null)
            throw new ArgumentException("Stream is null or empty.", nameof(stream));
        var source = new StreamByteSource(stream);
        return DeserializeCore(ref source, tagFactory, nameof(stream));
    }
    // Parsing runs in two passes over the input:
    //  1. Scan: read every identifier and length, validate the structure and settle each OCTET STRING
    //     encapsulation probe, without creating any tag. The final tree is recorded as a flat pre-order
    //     list of NodeRecords. A failed probe just truncates that list back to the probe itself.
    //  2. Materialize: create exactly the recorded tags, each container's children at their final
    //     size, and read only the content that ends up in the tree.
    // So nothing is allocated for structure that is later discarded, and invalid input allocates nothing
    // but the error message. The working buffers are rented from ArrayPool<T>.Shared for the duration of
    // one call; the parser keeps no state of its own between calls.

    /// <summary>Stateless, so one instance serves every call that does not pass its own factory.</summary>
    private static readonly DefaultTagFactory DefaultFactory = new();

    private static bool TryDeserializeCore<TSource>(ref TSource source, ITagFactory tagFactory, out ITag? tag, out string? error)
        where TSource : struct, IByteSource
    {
        try
        {
            error = null;
            if (source.Length == 0)
            {
                error = "Stream is null or empty.";
                tag = null;
                return false;
            }
            tag = DeserializeInternal(ref source, ref error, tagFactory);
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

    private static ITag DeserializeCore<TSource>(ref TSource source, ITagFactory tagFactory, string paramName)
        where TSource : struct, IByteSource
    {
        string? error = null;
        if (source.Length == 0)
            throw new ArgumentException("Stream is null or empty.", paramName);
        return DeserializeInternal(ref source, ref error, tagFactory) ?? throw new Asn1DeserializationException(error ?? "Unknown deserialization error");
    }

    private static ITag? DeserializeInternal<TSource>(ref TSource source, ref string? error, ITagFactory tagFactory)
        where TSource : struct, IByteSource
    {
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

    #region Byte sources

    /// <summary>
    /// The input of a parse. Implemented by structs and used through generic parameters, so the JIT emits
    /// a specialized parser per source with direct calls and nothing to allocate.
    /// </summary>
    private interface IByteSource
    {
        long Length { get; }
        long Position { get; set; }
        int ReadByte();
        int Read(byte[] buffer, int offset, int count);
    }

    /// <summary>A byte array read from offset 0.</summary>
    private struct ArrayByteSource(byte[] data) : IByteSource
    {
        private int _position;

        public readonly long Length => data.Length;

        public long Position
        {
            readonly get => _position;
            set => _position = (int)value;
        }

        public int ReadByte() => _position < data.Length ? data[_position++] : -1;

        public int Read(byte[] buffer, int offset, int count)
        {
            var n = Math.Min(count, data.Length - _position);
            if (n <= 0)
                return 0;
            Buffer.BlockCopy(data, _position, buffer, offset, n);
            _position += n;
            return n;
        }
    }

    /// <summary>A seekable stream, read from its current position.</summary>
    private readonly struct StreamByteSource(Stream stream) : IByteSource
    {
        private readonly long _length = stream.Length;

        public long Length => _length;

        public long Position
        {
            get => stream.Position;
            set => stream.Position = value;
        }

        public int ReadByte() => stream.ReadByte();

        public int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);
    }

    #endregion

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
    /// The default factory's traits for all 256 identifiers. Built once and never modified afterwards,
    /// so it is safe to share.
    /// </summary>
    private static readonly TagTraits[] DefaultTraits = BuildDefaultTraits();

    private static TagTraits[] BuildDefaultTraits()
    {
        var traits = new TagTraits[256];
        for (int i = 0; i < traits.Length; i++)
            traits[i] = TagTraits.Of(DefaultFactory.Create(i), i);
        return traits;
    }

    /// <summary>
    /// Traits per identifier for the factory of one parse. For a custom factory the table is rented for
    /// the call and each identifier is asked of the factory once, by creating a tag; the decisions depend
    /// only on a freshly created tag, as they always have.
    /// </summary>
    private readonly struct TraitsLookup
    {
        private readonly TagTraits[] _table;
        private readonly ITagFactory _factory;
        private readonly bool _rented;

        private TraitsLookup(TagTraits[] table, ITagFactory factory, bool rented)
        {
            _table = table;
            _factory = factory;
            _rented = rented;
        }

        public static TraitsLookup For(ITagFactory factory)
            => ReferenceEquals(factory, DefaultFactory)
                ? new TraitsLookup(DefaultTraits, factory, rented: false)
                : new TraitsLookup(ArrayPool<TagTraits>.Shared.Rent(256), factory, rented: true);

        public TagTraits this[int identifier]
        {
            get
            {
                ref var traits = ref _table[identifier];
                if (!traits.IsKnown)
                    traits = TagTraits.Of(_factory.Create(identifier), identifier);
                return traits;
            }
        }

        public void Release()
        {
            if (_rented)
                ArrayPool<TagTraits>.Shared.Return(_table, clearArray: true);
        }
    }

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
    }

    /// <summary>Entry of the materialize pass's parent stack.</summary>
    private struct OpenParent
    {
        /// <summary>Exact-size children array of a built-in tag, filled in place.</summary>
        public ITag[]? Array;

        /// <summary>Children list of a custom tag (or one the factory gave children), appended to.</summary>
        public IList<ITag>? List;

        public int Next;
        public int Remaining;
    }

    /// <summary>
    /// The buffers of one parse, rented from <see cref="ArrayPool{T}.Shared"/> and returned when the parse
    /// ends. Entries are handed out by reference so the structs are not copied.
    /// </summary>
    private struct ParseBuffers
    {
        public ContainerFrame[] Frames;
        public int FrameCount;
        public NodeRecord[] Records;
        public int RecordCount;
        public OpenParent[] Parents;

        public static ParseBuffers Rent() => new()
        {
            Frames = ArrayPool<ContainerFrame>.Shared.Rent(16),
            Records = ArrayPool<NodeRecord>.Shared.Rent(128),
            Parents = ArrayPool<OpenParent>.Shared.Rent(16),
        };

        public ref ContainerFrame Top => ref Frames[FrameCount - 1];

        public void Push(in ContainerFrame frame)
        {
            if (FrameCount == Frames.Length)
                Grow(ref Frames, FrameCount, clear: false);
            Frames[FrameCount++] = frame;
        }

        public ContainerFrame Pop() => Frames[--FrameCount];

        public int AddRecord(in NodeRecord record)
        {
            if (RecordCount == Records.Length)
                Grow(ref Records, RecordCount, clear: false);
            Records[RecordCount] = record;
            return RecordCount++;
        }

        public ref OpenParent ParentAt(int depth)
        {
            if (depth == Parents.Length)
                Grow(ref Parents, depth, clear: true);
            return ref Parents[depth];
        }

        /// <summary>
        /// Returns the buffers. Frames and records hold no references; the parent stack does, so it is cleared.
        /// </summary>
        public readonly void Return()
        {
            ArrayPool<ContainerFrame>.Shared.Return(Frames);
            ArrayPool<NodeRecord>.Shared.Return(Records);
            ArrayPool<OpenParent>.Shared.Return(Parents, clearArray: true);
        }

        private static void Grow<T>(ref T[] array, int used, bool clear)
        {
            var bigger = ArrayPool<T>.Shared.Rent(array.Length * 2);
            System.Array.Copy(array, bigger, used);
            ArrayPool<T>.Shared.Return(array, clear);
            array = bigger;
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

    /// <summary>
    /// Completes an element once its children have been scanned, successfully or not.
    /// </summary>
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

    /// <summary>
    /// Checks that a leaf's content is present and moves past it; it is read in the materialize pass.
    /// </summary>
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

    /// <summary>
    /// Reads a leaf's content. Streams may return fewer bytes than requested, so it reads until complete.
    /// </summary>
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

    /// <summary>
    /// Reads the length octets of the element whose identifier is at <paramref name="elementStart"/>.
    /// Returns <see langword="null"/> with <paramref name="valid"/> set for indefinite length; on failure
    /// <paramref name="valid"/> is <see langword="false"/> and <paramref name="error"/> says why.
    /// </summary>
    static long? DecodeLength<TSource>(ref TSource data, long elementStart, ref ParseError error, out bool valid)
        where TSource : struct, IByteSource
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
