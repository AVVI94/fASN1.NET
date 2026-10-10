using fASN1.NET.Factory;
using fASN1.NET.Tags;
using Xunit.Sdk;

namespace fASN1.NET.Tests.Infrastructure;

/// <summary>
/// Runs the production parser and <see cref="ReferenceRecursiveParser"/> on the same input and
/// requires identical observable behavior: success flag, error message, complete tree, and the
/// exact sequence of identifier bytes handed to the tag factory (which pins the read order,
/// including tags that were created and then discarded by a failed encapsulation probe).
/// </summary>
internal static class Differential
{
    public static void AssertSameAsReference(byte[] data, Func<ITagFactory>? factory = null)
    {
        factory ??= () => new DefaultTagFactory();
        var refFactory = new RecordingFactory(factory());
        var prodFactory = new RecordingFactory(factory());

        var refOk = ReferenceRecursiveParser.TryDeserialize(data, refFactory, out var refTag, out var refError);
        var prodOk = Asn1Serializer.TryDeserialize(data, prodFactory, out var prodTag, out var prodError);

        try
        {
            Assert.Equal(refOk, prodOk);
            Assert.Equal(refError, prodError);
            Assert.Equal(refFactory.Calls, prodFactory.Calls);
            if (refOk)
                TreeAssert.Equal(refTag!, prodTag!);
        }
        catch (XunitException ex)
        {
            throw new XunitException($"Input {Convert.ToHexString(data.AsSpan(0, Math.Min(data.Length, 256)))}{(data.Length > 256 ? "…" : "")} ({data.Length} bytes){Environment.NewLine}{ex.Message}");
        }
    }

    /// <summary>Records every <see cref="ITagFactory.Create"/> call and delegates to an inner factory.</summary>
    internal sealed class RecordingFactory(ITagFactory inner) : ITagFactory
    {
        public List<int> Calls { get; } = [];

        public ITag Create(int tag, byte[]? content = null)
        {
            Calls.Add(tag);
            return inner.Create(tag, content);
        }
    }

    /// <summary>Throws on the n-th call (1-based), to exercise exception propagation through containers.</summary>
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
