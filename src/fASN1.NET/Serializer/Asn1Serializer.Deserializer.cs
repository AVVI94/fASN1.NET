using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using fASN1.NET.Factory;
using fASN1.NET.Parsing;
using fASN1.NET.Tags;

namespace fASN1.NET;

/// <summary>
/// ASN.1 serializer.
/// </summary>
/// <remarks>
/// <c>TryDeserialize</c> never throws: it returns <see langword="false"/> with an error message whenever
/// <c>Deserialize</c> would throw. <c>Deserialize</c> throws <see cref="Asn1DeserializationException"/> when the data
/// is not a valid BER encoding, <see cref="ArgumentNullException"/> for a <see langword="null"/> argument,
/// <see cref="ArgumentException"/> for a stream that cannot seek, and lets any exception thrown by the stream or by a
/// custom <see cref="ITagFactory"/> propagate unchanged.
/// </remarks>
public static partial class Asn1Serializer
{
    #region Deserialize

    private const string EMPTY_DATA_MESSAGE = "The data is empty";

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified byte array.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; <see langword="false"/> if the data is not valid ASN.1 or could not be read. This method never throws.</returns>
    public static bool TryDeserialize(byte[] data, [NotNullWhen(true)] out ITag? tag)
        => TryDeserialize(data, out tag, out _);

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified byte array using the specified tag factory.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; <see langword="false"/> if the data is not valid ASN.1 or could not be read. This method never throws.</returns>
    public static bool TryDeserialize(byte[] data, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag)
        => TryDeserialize(data, tagFactory, out tag, out _);

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified stream.
    /// </summary>
    /// <param name="stream">The seekable stream containing the ASN.1 data, read from its current position.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; <see langword="false"/> if the data is not valid ASN.1 or could not be read. This method never throws.</returns>
    public static bool TryDeserialize(Stream stream, [NotNullWhen(true)] out ITag? tag)
        => TryDeserialize(stream, out tag, out _);

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified stream using the specified tag factory.
    /// </summary>
    /// <param name="stream">The seekable stream containing the ASN.1 data, read from its current position.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; <see langword="false"/> if the data is not valid ASN.1 or could not be read. This method never throws.</returns>
    public static bool TryDeserialize(Stream stream, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag)
        => TryDeserialize(stream, tagFactory, out tag, out _);

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified byte array.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <param name="error">When this method returns <see langword="false"/>, contains the error message, which is the exception's message when an argument is invalid or the stream or the tag factory throws; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; <see langword="false"/> if the data is not valid ASN.1 or could not be read. This method never throws.</returns>
    public static bool TryDeserialize(byte[] data, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        return TryDeserialize(data, DefaultTagFactory.Instance, out tag, out error);
    }

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified byte array using the specified tag factory.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <param name="error">When this method returns <see langword="false"/>, contains the error message, which is the exception's message when an argument is invalid or the stream or the tag factory throws; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; <see langword="false"/> if the data is not valid ASN.1 or could not be read. This method never throws.</returns>
    public static bool TryDeserialize(byte[] data, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        try
        {
            var source = CreateSource(data, tagFactory);
            return TryDeserializeCore(ref source, tagFactory, out tag, out error);
        }
        catch (Exception ex)
        {
            return Failed(ex, out tag, out error);
        }
    }

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified stream.
    /// </summary>
    /// <param name="stream">The seekable stream containing the ASN.1 data, read from its current position.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <param name="error">When this method returns <see langword="false"/>, contains the error message, which is the exception's message when an argument is invalid or the stream or the tag factory throws; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; <see langword="false"/> if the data is not valid ASN.1 or could not be read. This method never throws.</returns>
    public static bool TryDeserialize(Stream stream, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        return TryDeserialize(stream, DefaultTagFactory.Instance, out tag, out error);
    }

    /// <summary>
    /// Tries to deserialize the ASN.1 data from the specified stream using the specified tag factory.
    /// </summary>
    /// <param name="stream">The seekable stream containing the ASN.1 data, read from its current position.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the deserialized ASN.1 tag, or <see langword="null"/> if the deserialization fails.</param>
    /// <param name="error">When this method returns <see langword="false"/>, contains the error message, which is the exception's message when an argument is invalid or the stream or the tag factory throws; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the deserialization succeeds; <see langword="false"/> if the data is not valid ASN.1 or could not be read. This method never throws.</returns>
    public static bool TryDeserialize(Stream stream, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
    {
        try
        {
            var source = CreateSource(stream, tagFactory);
            return TryDeserializeCore(ref source, tagFactory, out tag, out error);
        }
        catch (Exception ex)
        {
            return Failed(ex, out tag, out error);
        }
    }

    /// <summary>
    /// Deserializes the ASN.1 data from the specified byte array.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <returns>The deserialized ASN.1 tag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="Asn1DeserializationException">The data is not valid ASN.1.</exception>
    public static ITag Deserialize(byte[] data)
    {
        return Deserialize(data, DefaultTagFactory.Instance);
    }

    /// <summary>
    /// Deserializes the ASN.1 data from the specified byte array using the specified tag factory.
    /// </summary>
    /// <param name="data">The byte array containing the ASN.1 data.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags. Its exceptions propagate.</param>
    /// <returns>The deserialized ASN.1 tag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> or <paramref name="tagFactory"/> is <see langword="null"/>.</exception>
    /// <exception cref="Asn1DeserializationException">The data is not valid ASN.1.</exception>
    public static ITag Deserialize(byte[] data, ITagFactory tagFactory)
    {
        var source = CreateSource(data, tagFactory);
        return DeserializeCore(ref source, tagFactory);
    }

    /// <summary>
    /// Deserializes the ASN.1 data from the specified stream.
    /// </summary>
    /// <param name="stream">The seekable stream containing the ASN.1 data, read from its current position. Its exceptions propagate.</param>
    /// <returns>The deserialized ASN.1 tag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> cannot seek.</exception>
    /// <exception cref="Asn1DeserializationException">The data is not valid ASN.1.</exception>
    public static ITag Deserialize(Stream stream)
    {
        return Deserialize(stream, DefaultTagFactory.Instance);
    }

    /// <summary>
    /// Deserializes the ASN.1 data from the specified stream using the specified tag factory.
    /// </summary>
    /// <param name="stream">The seekable stream containing the ASN.1 data, read from its current position. Its exceptions propagate.</param>
    /// <param name="tagFactory">The tag factory used to create ASN.1 tags. Its exceptions propagate.</param>
    /// <returns>The deserialized ASN.1 tag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="tagFactory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> cannot seek.</exception>
    /// <exception cref="Asn1DeserializationException">The data is not valid ASN.1.</exception>
    public static ITag Deserialize(Stream stream, ITagFactory tagFactory)
    {
        var source = CreateSource(stream, tagFactory);
        return DeserializeCore(ref source, tagFactory);
    }

    private static ByteArraySource CreateSource(byte[] data, ITagFactory tagFactory)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (tagFactory is null)
        {
            throw new ArgumentNullException(nameof(tagFactory));
        }

        return new ByteArraySource(data);
    }

    private static StreamSource CreateSource(Stream stream, ITagFactory tagFactory)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (tagFactory is null)
        {
            throw new ArgumentNullException(nameof(tagFactory));
        }

        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking.", nameof(stream));
        }

        return new StreamSource(stream);
    }

    private static bool TryDeserializeCore<TSource>(ref TSource source, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
        where TSource : struct, IByteSource
    {
        if (source.Length == 0)
        {
            tag = null;
            error = EMPTY_DATA_MESSAGE;
            return false;
        }

        return Asn1Parser.TryParse(ref source, tagFactory, out tag, out error);
    }

    /// <summary>A Try method's result for an exception: <see langword="false"/> with the exception's message.</summary>
    private static bool Failed(Exception exception, out ITag? tag, out string error)
    {
        tag = null;
        error = exception.Message;
        return false;
    }

    private static ITag DeserializeCore<TSource>(ref TSource source, ITagFactory tagFactory)
        where TSource : struct, IByteSource
    {
        return TryDeserializeCore(ref source, tagFactory, out var tag, out var error)
            ? tag
            : throw new Asn1DeserializationException(error);
    }
    #endregion
}
