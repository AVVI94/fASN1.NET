using System.Collections.Generic;
using System.Threading;

namespace fASN1.NET.Tags;

/// <summary>
/// Base class of the built-in tags. It stores only what varies per instance (content and children);
/// identity such as <see cref="TagNumber"/> and <see cref="TagName"/> is computed by each tag class, so a
/// parsed tag costs one small object.
/// </summary>
/// <remarks>
/// The children list is created on first use. A parsed leaf has no list until <see cref="Children"/> is
/// read, and the parser gives each container a list sized exactly to its children.
/// </remarks>
public abstract class TagBase : ITag
{
    private IList<ITag>? _children;

    /// <summary>
    /// Initializes the tag with optional content and children.
    /// </summary>
    /// <param name="content">The content bytes; an empty array when <see langword="null"/>.</param>
    /// <param name="children">
    /// The children list. It is used as is, not copied, so later changes to it are visible through <see cref="Children"/>.
    /// </param>
    protected TagBase(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        _children = children;
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
            var children = _children;
            if (children is null)
            {
                children = new List<ITag>();
                // Readers of a shared tree may get here concurrently; all of them must see the same list.
                children = Interlocked.CompareExchange(ref _children, children, null) ?? children;
            }
            return children;
        }
    }

    /// <inheritdoc/>
    public byte[] Content { get; set; }

    /// <inheritdoc/>
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }

    /// <summary>Number of children, without creating the list.</summary>
    internal int ChildCount => _children?.Count ?? 0;

    /// <summary>Sets the children of a freshly parsed tag that has none yet.</summary>
    internal void SetParsedChildren(List<ITag> children) => _children = children;

    /// <summary>Number of children of any tag, without creating a list for built-in tags.</summary>
    internal static int CountChildren(ITag tag) => tag is TagBase b ? b.ChildCount : tag.Children.Count;
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

    private static string[] Build(System.Func<int, string> name)
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
