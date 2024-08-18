using System;
using System.Collections.Generic;
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
            throw new Asn1DeserializationException("Stream is null or empty.");
        return DeserializeInternal(stream, ref error, tagFactory) ?? throw new Asn1DeserializationException(error ?? "Unknown deserialization error");
    }

    private static ITag? DeserializeInternal(Stream stream, ref string? error, ITagFactory tagFactory)
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
        var start = stream.Position;

        if (tag.IsConstructed || (tagNumber & 0x20) != 0)
        {
            if (!GetChildren(tag.Children, stream, ref length, ref error, tagFactory))
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
                if (!GetChildren(tag.Children, stream, ref length, ref error, tagFactory))
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

        if (tag.Children.Count == 0)
        {
            if (length is null)
            {
                error = "Cannot skip over an invalid tag with indefinite length";
                return null;
                //throw new InvalidDataException($"Cannot skip over an invalid tag with indefinite length at offset {start}");
            }
            //var position = stream.Position;
            var content = new byte[length.Value];
            if (length.Value > stream.Length - stream.Position)
            {
                error = $"Content size is not correct for tag {tag.TagName} at offset {start}";
                return null;
                //throw new InvalidDataException($"Content size is not correct for tag {tag.TagName} at offset {start}");
            }
            _ = stream.Read(content, 0, (int)length.Value);
            tag.Content = content;
        }
        return tag;
    }

    static bool GetChildren(IList<ITag> tags, Stream stream, ref long? length, ref string? error, ITagFactory tagFactory)
    {
        //List<ITag> tags = [];
        var start = stream.Position;
        if (length is not null)
        {
            var end = stream.Position + length.Value;
            if (end > stream.Length)
            {
                error = $"Container at offset {stream.Position} has a length of {length}, which is past the end of the stream";
                return false;
                //throw new InvalidDataException($"Container at offset {stream.Position} has a length of {length}, which is past the end of the stream");
            }
            while (stream.Position < end)
            {
                var child = DeserializeInternal(stream, ref error, tagFactory);
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
                var tag = DeserializeInternal(stream, ref error, tagFactory);
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

        return buf;
    }
    #endregion
}
