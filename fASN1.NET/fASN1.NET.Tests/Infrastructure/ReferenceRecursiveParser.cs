using System.Diagnostics.CodeAnalysis;
using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Tests.Infrastructure;

/// <summary>
/// Frozen copy of the original recursive deserializer (commit b921856), kept as a test oracle.
/// The production parser must produce exactly the same tree, the same success flag and the same
/// error message as this implementation for every input. Do not "fix" this file: it documents the
/// behavior the production parser is required to preserve.
/// </summary>
internal static class ReferenceRecursiveParser
{
    public static bool TryDeserialize(byte[] data, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        using var stream = new MemoryStream(data);
        try
        {
            error = null;
            if (stream is { Length: 0 })
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
        catch (Exception ex)
        {
            tag = null;
            error = ex.Message;
            return false;
        }
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

    private static bool GetChildren(IList<ITag> tags, Stream stream, ref long? length, ref string? error, ITagFactory tagFactory, long? parentContainerEnd)
    {
        var start = stream.Position;
        if (length is not null)
        {
            var end = stream.Position + length.Value;
            if (end > stream.Length || end > parentContainerEnd)
            {
                error = $"Container at offset {stream.Position} has a length of {length}, which is past the end of the stream";
                return false;
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
        }
    }

    private static int? DecodeLength(Stream data, out bool valid)
    {
        valid = true;
        var buf = data.ReadByte();
        var len = buf & 0x7f;

        if (len == buf)
            return len;
        if (len == 0)
            return null;
        if (len > 6)
        {
            valid = false;
            return null;
        }

        buf = 0;
        for (int y = 0; y < len; y++)
        {
            buf = (buf * 256) + data.ReadByte();
        }

        if (buf < 0)
        {
            valid = false;
            return null;
        }

        return buf;
    }
}
