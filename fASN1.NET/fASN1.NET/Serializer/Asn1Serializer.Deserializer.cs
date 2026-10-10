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
        /// A primitive OCTET STRING or BIT STRING whose content is tried as nested elements.
        /// A failure rolls the element back to a plain leaf.
        /// </summary>
        Probe,
    }

    /// <summary>
    /// A definite-length element whose children are being read.
    /// </summary>
    private sealed class ContainerFrame(FrameKind kind, ITag tag, long start, long childrenStart, long length)
    {
        public FrameKind Kind { get; } = kind;

        public ITag Tag { get; } = tag;

        /// <summary>Stream offset of the first content byte.</summary>
        public long Start { get; } = start;

        /// <summary>
        /// Stream offset of the first child. Equal to <see cref="Start"/> except for a BIT STRING probe,
        /// where the unused-bits byte comes first.
        /// </summary>
        public long ChildrenStart { get; } = childrenStart;

        /// <summary>Declared content length.</summary>
        public long Length { get; } = length;

        /// <summary>Offset where the children must end: <see cref="ChildrenStart"/> + <see cref="Length"/>.</summary>
        public long End => ChildrenStart + Length;
    }

    /// <summary>
    /// Parses one element and all of its descendants. Definite-length containers and encapsulation probes
    /// are tracked on an explicit stack instead of the call stack, so nesting depth does not consume call stack space.
    /// </summary>
    private static ITag? DeserializeInternal(Stream stream, ref string? error, ITagFactory tagFactory)
    {
        var frames = new Stack<ContainerFrame>();
        var needElement = true;
        ITag? completed = null;
        while (true)
        {
            if (needElement && BeginElement(stream, ref error, tagFactory, frames, out completed))
            {
                // A frame was opened; continue below with its first child.
            }
            else
            {
                // `completed` is a finished element, or null when it failed. Hand it to its parent.
                if (frames.Count == 0)
                {
                    return completed;
                }
                var parent = frames.Peek();
                if (completed is null)
                {
                    // A failed child ends the frame unsuccessfully, rewound to its first child.
                    stream.Position = parent.ChildrenStart;
                    completed = EndFrame(stream, ref error, frames.Pop(), success: false);
                    needElement = false;
                    continue;
                }
                parent.Tag.Children.Add(completed);
            }

            var top = frames.Peek();
            if (stream.Position < top.End)
            {
                needElement = true;
                continue;
            }

            frames.Pop();
            needElement = false;
            var exact = stream.Position == top.End;
            if (!exact)
            {
                error = $"Content size is not correct for container at offset {top.ChildrenStart}";
            }
            completed = EndFrame(stream, ref error, top, exact);
        }
    }

    /// <summary>
    /// Reads the identifier and length of the next element. When the element's children have to be read,
    /// a frame is pushed onto <paramref name="frames"/> and <see langword="true"/> is returned; any other
    /// element is parsed completely and returned through <paramref name="completed"/>.
    /// </summary>
    private static bool BeginElement(Stream stream, ref string? error, ITagFactory tagFactory, Stack<ContainerFrame> frames, out ITag? completed)
    {
        completed = null;
        var tagNumber = stream.ReadByte();
        if (tagNumber == -1)
        {
            return false;
        }
        var tag = tagFactory.Create(tagNumber);
        long? length = DecodeLength(stream, out var valid);
        if (!valid)
        {
            return false;
        }
        var start = stream.Position;
        if (length is null)
        {
            completed = ParseBodyRecursive(stream, ref error, tagFactory, tag, tagNumber, length, start);
            return false;
        }

        if (tag.IsConstructed || (tagNumber & 0x20) != 0)
        {
            return OpenFrame(stream, ref error, frames, new ContainerFrame(FrameKind.Container, tag, start, start, length.Value), out completed);
        }

        if (tag.IsUniversal && tag.TagNumber is (int)Tag.BitString or (int)Tag.OctetString)
        {
            if (tag.TagNumber is (int)Tag.BitString && stream.ReadByte() != 0)
            {
                error = "BitString with unused bits cannot encapsulate";
                RollBack(stream, tag, start);
                completed = FinishElement(stream, ref error, tag, start, length);
                return false;
            }
            return OpenFrame(stream, ref error, frames, new ContainerFrame(FrameKind.Probe, tag, start, stream.Position, length.Value), out completed);
        }

        completed = FinishElement(stream, ref error, tag, start, length);
        return false;
    }

    /// <summary>
    /// Checks that the frame's children fit inside the stream and inside the element itself, then pushes it.
    /// When they do not fit, the frame is ended unsuccessfully at once.
    /// </summary>
    private static bool OpenFrame(Stream stream, ref string? error, Stack<ContainerFrame> frames, ContainerFrame frame, out ITag? completed)
    {
        // For a BIT STRING probe the children start one byte late, so End always passes Start + Length.
        if (frame.End > stream.Length || frame.End > frame.Start + frame.Length)
        {
            error = $"Container at offset {frame.ChildrenStart} has a length of {frame.Length}, which is past the end of the stream";
            completed = EndFrame(stream, ref error, frame, success: false);
            return false;
        }
        frames.Push(frame);
        completed = null;
        return true;
    }

    /// <summary>
    /// Completes an element once its children have been read, successfully or not.
    /// </summary>
    private static ITag? EndFrame(Stream stream, ref string? error, ContainerFrame frame, bool success)
    {
        if (frame.Kind == FrameKind.Container)
        {
            return success ? FinishElement(stream, ref error, frame.Tag, frame.Start, frame.Length) : null;
        }

        if (!success)
        {
            RollBack(stream, frame.Tag, frame.Start);
        }
        else if (HasEocChild(frame.Tag))
        {
            RollBack(stream, frame.Tag, frame.Start);
            error = "EOC is not supposed to be actual content";
        }
        return FinishElement(stream, ref error, frame.Tag, frame.Start, frame.Length);
    }

    private static bool HasEocChild(ITag tag)
    {
        foreach (var child in tag.Children)
        {
            if (child.IsEoc)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Undoes a failed encapsulation probe: the element keeps no children and its content is read as raw bytes.
    /// </summary>
    private static void RollBack(Stream stream, ITag tag, long start)
    {
        tag.Children.Clear();
        stream.Position = start;
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

    private static ITag? DeserializeRecursive(Stream stream, ref string? error, ITagFactory tagFactory)
    {
        var tagNumber = stream.ReadByte();
        if (tagNumber == -1)
        {
            return null;
        }
        var tag = tagFactory.Create(tagNumber);
        long? length = DecodeLength(stream, out var valid);
        if (!valid)
        {
            return null;
        }
        return ParseBodyRecursive(stream, ref error, tagFactory, tag, tagNumber, length, stream.Position);
    }

    private static ITag? ParseBodyRecursive(Stream stream, ref string? error, ITagFactory tagFactory, ITag tag, int tagNumber, long? length, long start)
    {
        var tagMaxOffsetInStream = start + length;
        if (tag.IsConstructed || (tagNumber & 0x20) != 0)
        {
            if (!GetChildren(tag.Children, stream, ref length, ref error, tagFactory, tagMaxOffsetInStream))
            {
                return null;
            }
        }
        else if (tag.IsUniversal && tag.TagNumber is (int)Tag.BitString or (int)Tag.OctetString)
        {
            if (tag.TagNumber is (int)Tag.BitString && stream.ReadByte() != 0)
            {
                //throw new InvalidDataException("BitString with unused bits cannot encapsulate");
                error = "BitString with unused bits cannot encapsulate";
                Cleanup();
            }
            else
            {
                if (!GetChildren(tag.Children, stream, ref length, ref error, tagFactory, tagMaxOffsetInStream))
                {
                    Cleanup();
                }
                else
                {
                    foreach (var item in tag.Children)
                    {
                        if (item.IsEoc)
                        {
                            Cleanup();
                            error = "EOC is not supposed to be actual content";
                            break;
                        }
                        //throw new InvalidDataException("EOC is not supposed to be actual content");
                    }
                }
            }

            void Cleanup()
            {
                tag.Children.Clear();
                stream.Position = start;
            }
        }

        return FinishElement(stream, ref error, tag, start, length);
    }

    static bool GetChildren(IList<ITag> tags, Stream stream, ref long? length, ref string? error, ITagFactory tagFactory, long? parentContainerEnd)
    {
        //List<ITag> tags = [];
        var start = stream.Position;
        if (length is not null)
        {
            var end = stream.Position + length.Value;
            if (end > stream.Length || end > parentContainerEnd)
            {
                error = $"Container at offset {stream.Position} has a length of {length}, which is past the end of the stream";
                return false;
                //throw new InvalidDataException($"Container at offset {stream.Position} has a length of {length}, which is past the end of the stream");
            }
            while (stream.Position < end)
            {
                var child = DeserializeRecursive(stream, ref error, tagFactory);
                if (child is null)
                {
                    stream.Position = start;
                    return false;
                }
                else
                    tags.Add(child);
            }
            if (stream.Position != end)
            {
                error = $"Content size is not correct for container at offset {start}";
                return false;
                //throw new InvalidDataException($"Content size is not correct for container at offset {start}");
            }
            return true;
        }

        try
        {

            while (true)
            {
                var tag = DeserializeRecursive(stream, ref error, tagFactory);
                if (tag is null or { IsEoc: true })
                {
                    break;
                }
                tags.Add(tag);
            }
            length = stream.Position - start;
            return true;
        }
        catch (Exception)
        {
            error = $"Exception while decoding undefined length content at offset {start}";
            return false;
            //throw new Exception($"Exception while decoding undefined length content at offset {start}", ex);
        }
    }

    static int? DecodeLength(Stream data, out bool valid)
    {
        valid = true;
        var buf = (int)data.ReadByte();
        var len = buf & 0x7f;

        if (len == buf)
            return len;
        if (len == 0)
            return null;
        if (len > 6)
        {
            valid = false;
            return null; // Length over 48 bits not supported
        }

        buf = 0;
        for (int y = 0; y < len; y++)
        {
            buf = (buf * 256) + data.ReadByte();
        }

        if(buf < 0)
        {
            valid = false;
            return null;
        }

        return buf;
    }
    #endregion
}
