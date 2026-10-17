using System;
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
        {
            _payload = content;
        }
        else if (content is null || content.Length == 0)
        {
            _payload = children;
        }
        else
        {
            _payload = new ExternalWithContent(children, content);
        }
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
                {
                    return created;
                }
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
                    {
                        _payload = external.Children;
                    }
                    else
                    {
                        external.Content = value;
                    }

                    break;
                case ITag[] parsed:
                    if (value.Length != 0)
                    {
                        _payload = new ChildList(parsed, value);
                    }

                    break;
                case IList<ITag> supplied:
                    if (value.Length != 0)
                    {
                        _payload = new ExternalWithContent(supplied, value);
                    }

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
    internal ITag GetChild(int index) => _payload is ITag[] parsed ? parsed[index] : Children[index];

    /// <summary>
    /// Gives a freshly parsed tag, which has neither content nor children, an array to hold its children.
    /// </summary>
    internal ITag[] InitializeChildArray(int count)
    {
        var children = new ITag[count];
        _payload = children;
        return children;
    }

    /// <summary>Number of children of any tag, without creating a list for built-in tags.</summary>
    internal static int GetChildCount(ITag tag) => tag is TagBase b ? b.ChildCount : tag.Children.Count;

    /// <summary>A child of any tag, without creating a list for built-in tags.</summary>
    internal static ITag GetChild(ITag tag, int index) => tag is TagBase b ? b.GetChild(index) : tag.Children[index];

    private sealed class ExternalWithContent(IList<ITag> children, byte[] content)
    {
        public IList<ITag> Children { get; } = children;
        public byte[] Content { get; set; } = content;
    }
}
