using fASN1.NET.Factory;
using fASN1.NET.Tags;
using Xunit.Sdk;

namespace fASN1.NET.Tests.Infrastructure;

/// <summary>
/// The public contract every input must satisfy, valid or not:
/// <list type="bullet">
/// <item><c>TryDeserialize</c> never throws; it returns a tag, or <see langword="false"/> with an error message.</item>
/// <item><c>Deserialize</c> returns the same tree, or throws when <c>TryDeserialize</c> fails.</item>
/// <item>The byte-array and stream overloads agree, and the stream is read from its current position.</item>
/// <item>A parsed tree never has a node with both children and content.</item>
/// </list>
/// </summary>
internal static class Contract
{
    /// <summary>
    /// Checks the contract and returns the parsed tag, or <see langword="null"/> with the error.
    /// </summary>
    public static (ITag? Tag, string? Error) Check(byte[] data, Func<ITagFactory>? factory = null)
    {
        factory ??= () => new DefaultTagFactory();
        try
        {
            return CheckCore(data, factory);
        }
        catch (XunitException ex)
        {
            throw new XunitException($"Input {Convert.ToHexString(data.AsSpan(0, Math.Min(data.Length, 256)))}{(data.Length > 256 ? "…" : "")} ({data.Length} bytes){Environment.NewLine}{ex.Message}");
        }
    }

    private static (ITag? Tag, string? Error) CheckCore(byte[] data, Func<ITagFactory> factory)
    {
        var ok = Asn1Serializer.TryDeserialize(data, factory(), out var tag, out var error);

        if (ok)
        {
            Assert.NotNull(tag);
            TreeAssert.WellFormed(tag);
            // Serialize writes DER lengths and SET order; reading that back must give the same tree, every OCTET
            // STRING value included.
            var reserialized = Asn1Serializer.Serialize(tag);
            Assert.True(Asn1Serializer.TryDeserialize(reserialized, factory(), out var reparsed, out var reparseError), reparseError);
            TreeAssert.Equal(tag, reparsed!, setsInDerOrder: true);
            Assert.Equal(reserialized, Asn1Serializer.Serialize(reparsed!));
        }
        else
        {
            Assert.Null(tag);
            Assert.False(string.IsNullOrEmpty(error), "TryDeserialize failed without an error message");
        }

        // Same input behind a few junk bytes, read through the stream overload.
        var prefixed = new byte[data.Length + 3];
        prefixed[0] = 0xFF; prefixed[1] = 0x30; prefixed[2] = 0x80;
        data.CopyTo(prefixed, 3);
        using (var ms = new MemoryStream(prefixed) { Position = 3 })
        {
            var streamOk = Asn1Serializer.TryDeserialize(ms, factory(), out var streamTag, out var streamError);
            Assert.Equal(ok, streamOk);
            Assert.Equal(error is null, streamError is null); // offsets in the text are stream positions, so they shift
            if (ok)
                TreeAssert.Equal(tag!, streamTag!);
        }

        if (data.Length == 0)
        {
            Assert.Throws<ArgumentException>(() => Asn1Serializer.Deserialize(data, factory()));
        }
        else if (ok)
        {
            TreeAssert.Equal(tag!, Asn1Serializer.Deserialize(data, factory()));
        }
        else
        {
            var ex = Record.Exception(() => Asn1Serializer.Deserialize(data, factory()));
            Assert.NotNull(ex);
            if (ex is Asn1DeserializationException)
                Assert.Equal(error, ex.Message);
        }

        return (tag, error);
    }

    /// <summary>Throws on the n-th call (1-based), to exercise exception propagation.</summary>
    internal sealed class ThrowingFactory(int throwOnCall) : ITagFactory
    {
        private readonly DefaultTagFactory _inner = new();
        private int _calls;

        public ITag Create(int tag, byte[]? content = null)
        {
            if (++_calls == throwOnCall)
                throw new InvalidOperationException($"factory failure on call {throwOnCall}");
            return _inner.Create(tag, content);
        }
    }
}
