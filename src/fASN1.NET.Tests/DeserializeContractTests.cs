using fASN1.NET.Factory;
using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// What the Deserialize overloads do with everything but the data itself: <c>TryDeserialize</c> never throws and
/// reports any problem as <see langword="false"/> with a message; <c>Deserialize</c> throws.
/// </summary>
public class DeserializeContractTests
{
    private static readonly byte[] _valid = [0x30, 0x03, 0x02, 0x01, 0x05];

    [Fact]
    public void NullArguments_TryDeserializeReturnsFalse()
    {
        var factory = new DefaultTagFactory();

        AssertTryFails(Asn1Serializer.TryDeserialize((byte[])null!, out _), "data");
        AssertTryFails(Asn1Serializer.TryDeserialize((byte[])null!, out var tag, out var error), "data", tag, error);
        AssertTryFails(Asn1Serializer.TryDeserialize((byte[])null!, factory, out tag, out error), "data", tag, error);
        AssertTryFails(Asn1Serializer.TryDeserialize(_valid, null!, out tag, out error), "tagFactory", tag, error);
        AssertTryFails(Asn1Serializer.TryDeserialize((Stream)null!, out tag, out error), "stream", tag, error);
        AssertTryFails(Asn1Serializer.TryDeserialize((Stream)null!, factory, out tag, out error), "stream", tag, error);
        AssertTryFails(Asn1Serializer.TryDeserialize(new MemoryStream(_valid), null!, out tag, out error), "tagFactory", tag, error);
    }

    [Fact]
    public void NullArguments_DeserializeThrowsArgumentNullException()
    {
        var factory = new DefaultTagFactory();

        Assert.Equal("data", Assert.Throws<ArgumentNullException>(() => Asn1Serializer.Deserialize((byte[])null!)).ParamName);
        Assert.Equal("data", Assert.Throws<ArgumentNullException>(() => Asn1Serializer.Deserialize((byte[])null!, factory)).ParamName);
        Assert.Equal("tagFactory", Assert.Throws<ArgumentNullException>(() => Asn1Serializer.Deserialize(_valid, null!)).ParamName);
        Assert.Equal("stream", Assert.Throws<ArgumentNullException>(() => Asn1Serializer.Deserialize((Stream)null!)).ParamName);
        Assert.Equal("stream", Assert.Throws<ArgumentNullException>(() => Asn1Serializer.Deserialize((Stream)null!, factory)).ParamName);
        Assert.Equal("tagFactory", Assert.Throws<ArgumentNullException>(() => Asn1Serializer.Deserialize(new MemoryStream(_valid), null!)).ParamName);
    }

    [Fact]
    public void EmptyData_TryDeserializeReturnsFalse_DeserializeThrowsArgumentException()
    {
        Assert.False(Asn1Serializer.TryDeserialize([], out var tag, out var error));
        Assert.Null(tag);
        Assert.Equal("The data is empty", error);
        Assert.Equal("data", Assert.Throws<ArgumentException>(() => Asn1Serializer.Deserialize([])).ParamName);
        Assert.Equal("stream", Assert.Throws<ArgumentException>(() => Asn1Serializer.Deserialize(new MemoryStream())).ParamName);
    }

    [Fact]
    public void StreamThatCannotSeek_TryDeserializeReturnsFalse_DeserializeThrowsArgumentException()
    {
        AssertTryFails(Asn1Serializer.TryDeserialize(new TestStream(_valid, canSeek: false), out var tag, out var error), "stream", tag, error);
        Assert.Equal("stream", Assert.Throws<ArgumentException>(() => Asn1Serializer.Deserialize(new TestStream(_valid, canSeek: false))).ParamName);
    }

    [Fact]
    public void StreamException_TryDeserializeReturnsFalse_DeserializePropagatesIt()
    {
        Assert.False(Asn1Serializer.TryDeserialize(new TestStream(_valid, failReads: true), out var tag, out var error));
        Assert.Null(tag);
        Assert.Equal(TestStream.FAILURE_MESSAGE, error);
        Assert.Equal(TestStream.FAILURE_MESSAGE, Assert.Throws<IOException>(() => Asn1Serializer.Deserialize(new TestStream(_valid, failReads: true))).Message);
    }

    [Fact]
    public void FactoryException_TryDeserializeReturnsFalse_DeserializePropagatesIt()
    {
        var data = Convert.FromHexString("3006020105020107");

        Assert.False(Asn1Serializer.TryDeserialize(data, new Contract.ThrowingFactory(2), out var tag, out var error));
        Assert.Null(tag);
        Assert.Equal("factory failure on call 2", error);
        Assert.False(Asn1Serializer.TryDeserialize(new MemoryStream(data), new Contract.ThrowingFactory(2), out tag, out error));
        Assert.Equal("factory failure on call 2", error);
        Assert.Throws<InvalidOperationException>(() => Asn1Serializer.Deserialize(data, new Contract.ThrowingFactory(2)));
    }

    private static void AssertTryFails(bool result, string paramName, ITag? tag = null, string? error = null)
    {
        Assert.False(result);
        Assert.Null(tag);
        if (error is not null)
        {
            Assert.Contains(paramName, error);
        }
    }

    /// <summary>A stream over fixed data that can refuse to seek or fail every read.</summary>
    private sealed class TestStream(byte[] data, bool canSeek = true, bool failReads = false) : MemoryStream(data)
    {
        public const string FAILURE_MESSAGE = "The device is not ready";

        public override bool CanSeek => canSeek;

        public override int Read(byte[] buffer, int offset, int count)
            => failReads ? throw new IOException(FAILURE_MESSAGE) : base.Read(buffer, offset, count);

        public override int ReadByte() => failReads ? throw new IOException(FAILURE_MESSAGE) : base.ReadByte();
    }
}
