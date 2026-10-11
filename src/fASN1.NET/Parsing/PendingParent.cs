using System.Collections.Generic;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>A tag created by <see cref="TreeBuilder"/> that is still waiting for some of its children.</summary>
internal struct PendingParent
{
    /// <summary>The exact-size children array of a built-in tag, filled in place; otherwise <see langword="null"/>.</summary>
    public ITag[]? ChildArray;

    /// <summary>
    /// The children list of a custom tag, or of one the factory created with children already, which is
    /// appended to instead.
    /// </summary>
    public IList<ITag>? ChildList;

    /// <summary>Index in <see cref="ChildArray"/> where the next child goes.</summary>
    public int NextChildIndex;

    public int RemainingChildren;

    public void Add(ITag child)
    {
        if (ChildArray is not null)
            ChildArray[NextChildIndex++] = child;
        else
            ChildList!.Add(child);
        RemainingChildren--;
    }
}
