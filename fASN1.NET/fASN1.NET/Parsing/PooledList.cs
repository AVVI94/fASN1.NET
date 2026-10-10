using System;
using System.Buffers;

namespace fASN1.NET.Parsing;

/// <summary>
/// A growable list whose storage is rented from <see cref="ArrayPool{T}.Shared"/> and must be returned
/// with <see cref="Dispose"/>. Items are handed out by reference so that structs are not copied.
/// </summary>
/// <remarks>
/// A mutable struct: keep it in a field or a non-readonly local, and never in a <c>using</c> variable,
/// which would make every call work on a copy.
/// </remarks>
internal struct PooledList<T>
{
    private T[] _items;
    private readonly bool _clearOnRemove;

    /// <param name="initialCapacity">Capacity to rent up front.</param>
    /// <param name="clearOnRemove">
    /// Clear removed slots and the array on return. Needed when <typeparamref name="T"/> holds references,
    /// so the pool does not keep objects alive.
    /// </param>
    public PooledList(int initialCapacity, bool clearOnRemove)
    {
        _items = ArrayPool<T>.Shared.Rent(initialCapacity);
        _clearOnRemove = clearOnRemove;
        Count = 0;
    }

    public int Count { get; private set; }

    public readonly ref T this[int index] => ref _items[index];

    public readonly ref T Last => ref _items[Count - 1];

    /// <summary>Adds an item and returns its index.</summary>
    public int Add(in T item)
    {
        if (Count == _items.Length)
            Grow();
        _items[Count] = item;
        return Count++;
    }

    public T RemoveLast()
    {
        var item = _items[--Count];
        if (_clearOnRemove)
            _items[Count] = default!;
        return item;
    }

    /// <summary>Removes every item from <paramref name="count"/> onwards.</summary>
    public void Truncate(int count)
    {
        if (_clearOnRemove)
            Array.Clear(_items, count, Count - count);
        Count = count;
    }

    public readonly ReadOnlySpan<T> AsSpan() => new(_items, 0, Count);

    public void Dispose()
    {
        if (_items is null)
            return;
        ArrayPool<T>.Shared.Return(_items, _clearOnRemove);
        _items = null!;
        Count = 0;
    }

    private void Grow()
    {
        var larger = ArrayPool<T>.Shared.Rent(_items.Length * 2);
        Array.Copy(_items, larger, Count);
        ArrayPool<T>.Shared.Return(_items, _clearOnRemove);
        _items = larger;
    }
}
