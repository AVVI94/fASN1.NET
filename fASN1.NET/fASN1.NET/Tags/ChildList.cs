using System;
using System.Collections;
using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The mutable children list of a built-in tag. It starts on the array the parser filled (without copying)
/// and grows like <see cref="List{T}"/> when children are added. It also holds the tag's content once a
/// list exists, so a tag with both still costs a single extra object.
/// </summary>
internal sealed class ChildList : IList<ITag>, IReadOnlyList<ITag>
{
    private ITag[] _items;
    private int _count;
    private int _version;

    public ChildList(ITag[] items, byte[]? content)
    {
        _items = items;
        _count = items.Length;
        Content = content;
    }

    /// <summary>Content of the owning tag; <see langword="null"/> when empty.</summary>
    public byte[]? Content { get; set; }

    public int Count => _count;

    public bool IsReadOnly => false;

    public ITag this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _items[index];
        }
        set
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _items[index] = value;
            _version++;
        }
    }

    public void Add(ITag item)
    {
        if (_count == _items.Length)
            Grow();
        _items[_count++] = item;
        _version++;
    }

    public void Insert(int index, ITag item)
    {
        if ((uint)index > (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (_count == _items.Length)
            Grow();
        Array.Copy(_items, index, _items, index + 1, _count - index);
        _items[index] = item;
        _count++;
        _version++;
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
        _count--;
        Array.Copy(_items, index + 1, _items, index, _count - index);
        _items[_count] = null!;
        _version++;
    }

    public bool Remove(ITag item)
    {
        var index = IndexOf(item);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

    public void Clear()
    {
        Array.Clear(_items, 0, _count);
        _count = 0;
        _version++;
    }

    public int IndexOf(ITag item) => Array.IndexOf(_items, item, 0, _count);

    public bool Contains(ITag item) => IndexOf(item) >= 0;

    public void CopyTo(ITag[] array, int arrayIndex) => Array.Copy(_items, 0, array, arrayIndex, _count);

    public IEnumerator<ITag> GetEnumerator()
    {
        var version = _version;
        for (int i = 0; i < _count; i++)
        {
            if (version != _version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            yield return _items[i];
        }
        if (version != _version)
            throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Grow() => Array.Resize(ref _items, _items.Length == 0 ? 4 : _items.Length * 2);
}
