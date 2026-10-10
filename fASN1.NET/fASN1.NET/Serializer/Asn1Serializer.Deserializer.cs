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
        return TryDeserialize(data, new DefaultTagFactory(), out tag, out error);
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
        return TryDeserialize(stream, new DefaultTagFactory(), out tag, out error);
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
        return Deserialize(data, new DefaultTagFactory());
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
        return Deserialize(stream, new DefaultTagFactory());
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

    /// <summary>
    /// What a frame's children are being read for.
    /// </summary>
    private enum FrameKind
    {
        /// <summary>A constructed element; a failure in its children fails the element.</summary>
        Container,

        /// <summary>
        /// A primitive OCTET STRING whose content is tried as nested elements.
        /// A failure rolls the element back to a plain leaf.
        /// </summary>
        Probe,
    }

    /// <summary>
    /// An element whose children are being read. A struct, so opening a frame does not allocate.
    /// </summary>
    private struct ContainerFrame(FrameKind kind, ITag tag, long start, long? length)
    {
        public FrameKind Kind { get; } = kind;

        public ITag Tag { get; } = tag;

        /// <summary>Stream offset of the first content byte.</summary>
        public long Start { get; } = start;

        /// <summary>Children are read until an end-of-contents marker instead of up to <see cref="End"/>.</summary>
        public bool IsIndefinite { get; } = length is null;

        /// <summary>Declared content length; <see langword="null"/> for indefinite length.</summary>
        public long? Length { get; } = length;

        /// <summary>Offset where definite-length children must end.</summary>
        public long End => Start + Length!.Value;

        /// <summary>A probe saw an EOC among its direct children, so its content is not a run of elements.</summary>
        public bool ContainsEoc { get; set; }
    }

    /// <summary>
    /// What <see cref="BeginElement"/> did with the next element.
    /// </summary>
    private enum Step
    {
        /// <summary>The element was parsed completely (or failed).</summary>
        Completed,

        /// <summary>A frame was pushed; its children come next.</summary>
        Opened,

        /// <summary>The end-of-contents marker of the innermost indefinite-length frame was read.</summary>
        EndOfContents,
    }

    /// <summary>
    /// Parses one element and all of its descendants. Every element whose children are being read is a
    /// <see cref="ContainerFrame"/> on an explicit stack, so nesting depth does not consume call stack space.
    /// </summary>
    private static ITag? DeserializeInternal(Stream stream, ref string? error, ITagFactory tagFactory)
    {
        var frames = new FrameStack();
        var needElement = true;
        ITag? completed = null;
        while (true)
        {
            var step = needElement ? BeginElement(stream, ref error, tagFactory, frames, out completed) : Step.Completed;
            if (step == Step.EndOfContents)
            {
                var ended = frames.Pop();
                completed = EndFrame(stream, ref error, in ended, success: true);
                needElement = false;
                continue;
            }
            if (step == Step.Completed)
            {
                // `completed` is a finished element, or null when it failed. Hand it to its parent.
                if (frames.Count == 0)
                {
                    return completed;
                }
                if (completed is null)
                {
                    // A failed child ends the frame unsuccessfully.
                    var failed = frames.Pop();
                    completed = EndFrame(stream, ref error, in failed, success: false);
                    needElement = false;
                    continue;
                }
                frames.Top.Tag.Children.Add(completed);
            }

            ref var top = ref frames.Top;
            if (top.IsIndefinite || stream.Position < top.End)
            {
                needElement = true;
                continue;
            }

            var done = frames.Pop();
            needElement = false;
            var exact = stream.Position == done.End;
            if (!exact)
            {
                error = $"Content size is not correct for container at offset {done.Start}";
            }
            completed = EndFrame(stream, ref error, in done, exact);
        }
    }

    /// <summary>
    /// Array-backed stack of frames that hands out the top frame by reference, so frames are not copied
    /// on every access the way <see cref="Stack{T}.Peek"/> would copy them.
    /// </summary>
    private sealed class FrameStack
    {
        private ContainerFrame[] _items = new ContainerFrame[16];

        public int Count { get; private set; }

        public ref ContainerFrame Top => ref _items[Count - 1];

        public void Push(in ContainerFrame frame)
        {
            if (Count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }
            _items[Count++] = frame;
        }

        public ContainerFrame Pop()
        {
            var frame = _items[--Count];
            _items[Count] = default;
            return frame;
        }
    }

    /// <summary>
    /// Reads the identifier and length of the next element. A complete element (or a failure) is returned
    /// through <paramref name="completed"/>; an element whose children have to be read is pushed onto
    /// <paramref name="frames"/>.
    /// </summary>
    private static Step BeginElement(Stream stream, ref string? error, ITagFactory tagFactory, FrameStack frames, out ITag? completed)
    {
        completed = null;
        var elementStart = stream.Position;
        var tagNumber = stream.ReadByte();
        var inIndefinite = frames.Count > 0 && frames.Top.IsIndefinite;
        if (tagNumber == -1)
        {
            if (inIndefinite)
            {
                error = $"Indefinite-length container at offset {frames.Top.Start} has no end-of-contents marker";
            }
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
                error = $"Invalid end-of-contents marker at offset {elementStart}";
                return Step.Completed;
            }
            if (frames.Count > 0 && frames.Top.Kind == FrameKind.Probe)
            {
                // EOC cannot be content, so this OCTET STRING does not wrap nested elements.
                frames.Top.ContainsEoc = true;
            }
        }

        var tag = tagFactory.Create(tagNumber);
        var start = stream.Position;

        if (tag.IsConstructed || (tagNumber & 0x20) != 0)
        {
            return OpenFrame(stream, ref error, frames, new ContainerFrame(FrameKind.Container, tag, start, length), out completed);
        }

        // An OCTET STRING may wrap DER (X.509 extension values do), so its content is tried as nested elements.
        // A BIT STRING is always a leaf: signatures and keys would otherwise be expanded by accident.
        if (length is not null && tag.IsUniversal && tag.TagNumber == (int)Tag.OctetString)
        {
            return OpenFrame(stream, ref error, frames, new ContainerFrame(FrameKind.Probe, tag, start, length), out completed);
        }

        completed = FinishElement(stream, ref error, tag, start, length);
        return Step.Completed;
    }

    /// <summary>
    /// Pushes the frame. A definite-length frame is first checked to fit inside the stream;
    /// when it does not, it is ended unsuccessfully at once.
    /// </summary>
    private static Step OpenFrame(Stream stream, ref string? error, FrameStack frames, ContainerFrame frame, out ITag? completed)
    {
        if (!frame.IsIndefinite && frame.End > stream.Length)
        {
            error = $"Container at offset {frame.Start} has a length of {frame.Length}, which is past the end of the stream";
            completed = EndFrame(stream, ref error, in frame, success: false);
            return Step.Completed;
        }
        frames.Push(frame);
        completed = null;
        return Step.Opened;
    }

    /// <summary>
    /// Completes an element once its children have been read, successfully or not.
    /// </summary>
    private static ITag? EndFrame(Stream stream, ref string? error, in ContainerFrame frame, bool success)
    {
        if (frame.Kind == FrameKind.Container)
        {
            if (!success)
                return null;
            // An indefinite-length container has no content of its own, even when it has no children.
            return frame.IsIndefinite ? frame.Tag : FinishElement(stream, ref error, frame.Tag, frame.Start, frame.Length);
        }

        if (!success || frame.ContainsEoc)
        {
            // Not a complete run of nested elements: keep the content as raw bytes.
            frame.Tag.Children.Clear();
            stream.Position = frame.Start;
        }
        return FinishElement(stream, ref error, frame.Tag, frame.Start, frame.Length);
    }

    /// <summary>
    /// Shared tail of every element: a node without children takes its content bytes from the stream.
    /// </summary>
    private static ITag? FinishElement(Stream stream, ref string? error, ITag tag, long start, long? length)
    {
        if (tag.Children.Count == 0)
        {
            if (length is null)
            {
                error = "Cannot skip over an invalid tag with indefinite length";
                return null;
            }
            if (length.Value > stream.Length - stream.Position)
            {
                error = $"Content size is not correct for tag {tag.TagName} at offset {start}";
                return null;
            }
            var content = new byte[length.Value];
            _ = stream.Read(content, 0, (int)length.Value);
            tag.Content = content;
        }
        return tag;
    }

    /// <summary>
    /// Reads the length octets of the element whose identifier is at <paramref name="elementStart"/>.
    /// Returns <see langword="null"/> with <paramref name="valid"/> set for indefinite length; on failure
    /// <paramref name="valid"/> is <see langword="false"/> and <paramref name="error"/> says why.
    /// </summary>
    static long? DecodeLength(Stream data, long elementStart, ref string? error, out bool valid)
    {
        valid = false;
        var first = data.ReadByte();
        if (first == -1)
        {
            error = $"Unexpected end of data in the length of the element at offset {elementStart}";
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
            error = $"Length of the element at offset {elementStart} uses {octets} octets; at most 6 are supported";
            return null;
        }

        long value = 0;
        for (int i = 0; i < octets; i++)
        {
            var b = data.ReadByte();
            if (b == -1)
            {
                error = $"Unexpected end of data in the length of the element at offset {elementStart}";
                return null;
            }
            value = (value << 8) | (uint)b;
        }

        // Content is read into a byte[], so lengths are limited to what an array can index.
        if (value > int.MaxValue)
        {
            error = $"Length of the element at offset {elementStart} is too large";
            return null;
        }
        valid = true;
        return value;
    }
    #endregion
}
