using fASN1.NET.Factory;
using fASN1.NET.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace fASN1.NET;

/// <summary>
/// ASN.1 serializer.
/// </summary>
public static partial class Asn1Serializer
{
    #region Deserialize

    /// <summary>
    /// Deserializes the ASN.1 data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing the ASN.1 data.<br/>
    /// This stream must have the <see cref="Stream.CanSeek"/> property set to <see langword="true"/>.
    /// </param>    
    /// <returns>The deserialized ASN.1 tag.</returns>
    public static ITag? Deserialize(Stream stream)
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
    public static ITag? Deserialize(Stream stream, ITagFactory tagFactory)
    {
        return DeserializeInternal(stream, tagFactory);
    }

    private static ITag? DeserializeInternal(Stream stream, ITagFactory tagFactory)
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
            if (!GetChildren(tag.Children, stream, ref length, tagFactory))
            {
                return null;
            }
        }
        else if (tag.IsUniversal && tag.TagNumber is (int)Tag.BitString or (int)Tag.OctetString)
        {
            if (tag.TagNumber is (int)Tag.BitString && stream.ReadByte() != 0)
            {
                //throw new InvalidDataException("BitString with unused bits cannot encapsulate");
                Cleanup();
            }
            else
            {
                if (!GetChildren(tag.Children, stream, ref length, tagFactory))
                {
                    Cleanup();
                }
                else
                {
                    foreach (var item in tag.Children)
                    {
                        if (item.IsEoc)
                            Cleanup();
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
                return null;
                //throw new InvalidDataException($"Cannot skip over an invalid tag with indefinite length at offset {start}");
            }
            //var position = stream.Position;
            var content = new byte[length.Value];
            _ = stream.Read(content, 0, (int)length.Value);
            tag.Content = content;
        }
        return tag;
    }

    static bool GetChildren(IList<ITag> tags, Stream stream, ref long? length, ITagFactory tagFactory)
    {
        //List<ITag> tags = [];
        var start = stream.Position;
        if (length is not null)
        {
            var end = stream.Position + length.Value;
            if (end > stream.Length)
            {
                return false;
                //throw new InvalidDataException($"Container at offset {stream.Position} has a length of {length}, which is past the end of the stream");
            }
            while (stream.Position < end)
            {
                var child = DeserializeInternal(stream, tagFactory);
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
                return false;
                //throw new InvalidDataException($"Content size is not correct for container at offset {start}");
            }
            return true;
        }

        try
        {

            while (true)
            {
                var tag = DeserializeInternal(stream, tagFactory);
                if (tag is null or { IsEoc: true })
                {
                    break;
                }
                tags.Add(tag);
            }
            length = stream.Position - start;
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Exception while decoding undefined length content at offset {start}", ex);
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
