using System;
using System.IO;

namespace fASN1.NET;

/// <summary>
/// A minimal read-only, seekable stream over a byte array, reused per thread by the byte-array overloads
/// so that parsing an array does not allocate a <see cref="MemoryStream"/> per call.
/// </summary>
internal sealed class ArraySource : Stream
{
    [ThreadStatic]
    private static ArraySource? t_cached;

    private byte[] _data = [];
    private int _position;

    /// <summary>
    /// Takes the thread's instance (or a new one, for a nested parse) and points it at <paramref name="data"/>.
    /// </summary>
    public static ArraySource Rent(byte[] data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        var source = t_cached ?? new ArraySource();
        t_cached = null;
        source._data = data;
        source._position = 0;
        return source;
    }

    /// <summary>Releases the caller's array and keeps the instance for the next parse on this thread.</summary>
    public static void Return(ArraySource source)
    {
        source._data = [];
        source._position = 0;
        t_cached = source;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _data.Length;

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));
            _position = (int)value;
        }
    }

    public override int ReadByte() => _position < _data.Length ? _data[_position++] : -1;

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = Math.Min(count, _data.Length - _position);
        if (n <= 0)
            return 0;
        Buffer.BlockCopy(_data, _position, buffer, offset, n);
        _position += n;
        return n;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            _ => _data.Length + offset,
        };
        return _position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
