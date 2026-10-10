using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace fASN1.NET.Tags;

/// <summary>
/// Base class of the built-in tags. It stores only what varies per instance (content and children), in a
/// single field; identity such as <see cref="TagNumber"/> and <see cref="TagName"/> is computed by each tag
/// class, so a parsed tag costs one small object.
/// </summary>
/// <remarks>
/// A parsed container keeps its children in an array of exactly the right size, and a list object is
/// created only when <see cref="Children"/> is read. That list is fully mutable.
/// </remarks>
public abstract class TagBase : ITag
{
    // One of:
    //   null                  no content, no children
    //   byte[]                content, no children
    //   ITag[]                children from the parser, not yet wrapped in a list; no content
    //   ChildList             children list (it also carries the content, if any)
    //   IList<ITag>           a list passed to the constructor, used by reference; no content
    //   ExternalWithContent   such a list together with content
    private object? _payload;

    /// <summary>
    /// Initializes the tag with optional content and children.
    /// </summary>
    /// <param name="content">The content bytes; an empty array when <see langword="null"/>.</param>
    /// <param name="children">
    /// The children list. It is used as is, not copied, so later changes to it are visible through <see cref="Children"/>.
    /// </param>
    protected TagBase(byte[]? content = null, IList<ITag>? children = null)
    {
        if (children is null)
            _payload = content;
        else if (content is null || content.Length == 0)
            _payload = children;
        else
            _payload = new ExternalWithContent(children, content);
    }

    /// <inheritdoc/>
    public abstract int TagNumber { get; }

    /// <inheritdoc/>
    public abstract string TagName { get; }

    /// <inheritdoc/>
    public abstract int TagClass { get; }

    /// <inheritdoc/>
    /// <remarks>By default a tag counts as constructed when it has children and no content.</remarks>
    public virtual bool IsConstructed => Content.Length == 0 && ChildCount > 0;

    /// <inheritdoc/>
    public virtual bool IsUniversal => TagClass == 0;

    /// <inheritdoc/>
    public virtual bool IsEoc => false;

    /// <inheritdoc/>
    public IList<ITag> Children
    {
        get
        {
            while (true)
            {
                var payload = Volatile.Read(ref _payload);
                switch (payload)
                {
                    case IList<ITag> list and not ITag[]:
                        return list;
                    case ExternalWithContent external:
                        return external.Children;
                }
                // Readers of a shared tree may get here concurrently; all of them must see the same list.
                var created = payload switch
                {
                    ITag[] parsed => new ChildList(parsed, null),
                    byte[] content => new ChildList([], content),
                    _ => new ChildList([], null),
                };
                if (ReferenceEquals(Interlocked.CompareExchange(ref _payload, created, payload), payload))
                    return created;
            }
        }
    }

    /// <inheritdoc/>
    public byte[] Content
    {
        get => _payload switch
        {
            byte[] content => content,
            ChildList list => list.Content ?? [],
            ExternalWithContent external => external.Content,
            _ => [],
        };
        set
        {
            switch (_payload)
            {
                case null or byte[]:
                    _payload = value;
                    break;
                case ChildList list:
                    list.Content = value;
                    break;
                case ExternalWithContent external:
                    if (value.Length == 0)
                        _payload = external.Children;
                    else
                        external.Content = value;
                    break;
                case ITag[] parsed:
                    if (value.Length != 0)
                        _payload = new ChildList(parsed, value);
                    break;
                case IList<ITag> supplied:
                    if (value.Length != 0)
                        _payload = new ExternalWithContent(supplied, value);
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }

    /// <summary>Number of children, without creating a list.</summary>
    internal int ChildCount => _payload switch
    {
        ITag[] parsed => parsed.Length,
        ICollection<ITag> list => list.Count,
        ExternalWithContent external => external.Children.Count,
        _ => 0,
    };

    /// <summary>The child at <paramref name="index"/>, without creating a list.</summary>
    internal ITag ChildAt(int index) => _payload is ITag[] parsed ? parsed[index] : Children[index];

    /// <summary>
    /// Gives a freshly parsed tag, which has neither content nor children, an array to hold its children.
    /// </summary>
    internal ITag[] SetParsedChildren(int count)
    {
        var children = new ITag[count];
        _payload = children;
        return children;
    }

    /// <summary>Number of children of any tag, without creating a list for built-in tags.</summary>
    internal static int CountChildren(ITag tag) => tag is TagBase b ? b.ChildCount : tag.Children.Count;

    /// <summary>A child of any tag, without creating a list for built-in tags.</summary>
    internal static ITag ChildAt(ITag tag, int index) => tag is TagBase b ? b.ChildAt(index) : tag.Children[index];

    private sealed class ExternalWithContent(IList<ITag> children, byte[] content)
    {
        public IList<ITag> Children { get; } = children;
        public byte[] Content { get; set; } = content;
    }
}

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

/// <summary>
/// Cached names for tags whose name depends on the tag number, so that creating such a tag does not
/// allocate a string.
/// </summary>
internal static class TagNames
{
    private static readonly string[] Context = Build(n => $"[{n}]");
    private static readonly string[] ApplicationNames = Build(n => $"Application_{n}");
    private static readonly string[] PrivateNames = Build(n => $"Private_{n}");
    private static readonly string[] UnknownNames = Build(n => $"Unknown_{n}");

    private static string[] Build(Func<int, string> name)
    {
        var names = new string[32];
        for (int i = 0; i < names.Length; i++)
            names[i] = name(i);
        return names;
    }

    public static string ContextSpecific(int tagNumber) => Context[tagNumber & 0x1F];
    public static string Application(int tagNumber) => ApplicationNames[tagNumber & 0x1F];
    public static string Private(int tagNumber) => PrivateNames[tagNumber & 0x1F];
    public static string Unknown(int tagNumber) => UnknownNames[tagNumber & 0x1F];
}
