using System;
using System.Buffers;
using System.Collections.Generic;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>One node of the final tree, in pre-order, as found by the scan.</summary>
internal struct NodeRecord
{
    public int Identifier;

    /// <summary>Stream offset of the first content byte.</summary>
    public long Start;

    /// <summary>Content length of a leaf.</summary>
    public long Length;

    /// <summary>Children were read; the next <see cref="ChildCount"/> subtrees in pre-order are them.</summary>
    public bool IsContainer;

    public bool IsIndefinite;

    public int ChildCount;

    /// <summary>A probe that did not hold nested elements: a leaf whose content is the raw bytes.</summary>
    public bool RolledBack;
}

/// <summary>What a frame's children are being read for.</summary>
internal enum FrameKind
{
    /// <summary>A constructed element; a failure in its children fails the element.</summary>
    Container,

    /// <summary>A primitive OCTET STRING whose content is tried as nested elements; a failure rolls it back to a leaf.</summary>
    Probe,
}

/// <summary>An element whose children are being scanned.</summary>
internal struct ContainerFrame
{
    public FrameKind Kind;

    /// <summary>This element's entry in the record list.</summary>
    public int Record;

    /// <summary>Stream offset of the first content byte.</summary>
    public long Start;

    /// <summary>Declared content length; <see langword="null"/> for indefinite length.</summary>
    public long? Length;

    public readonly bool IsIndefinite => Length is null;

    /// <summary>Offset where definite-length children must end.</summary>
    public readonly long End => Start + Length!.Value;

    /// <summary>Number of direct children scanned so far.</summary>
    public int ChildCount;

    /// <summary>A probe saw an EOC among its direct children, so its content is not a run of elements.</summary>
    public bool ContainsEoc;
}

/// <summary>Entry of the materialize pass's parent stack.</summary>
internal struct OpenParent
{
    /// <summary>Exact-size children array of a built-in tag, filled in place.</summary>
    public ITag[]? Array;

    /// <summary>Children list of a custom tag (or one the factory gave children), appended to.</summary>
    public IList<ITag>? List;

    public int Next;
    public int Remaining;
}

/// <summary>
/// The buffers of one parse, rented from <see cref="ArrayPool{T}.Shared"/> and returned when the parse
/// ends. Entries are handed out by reference so the structs are not copied.
/// </summary>
internal struct ParseBuffers
{
    public ContainerFrame[] Frames;
    public int FrameCount;
    public NodeRecord[] Records;
    public int RecordCount;
    public OpenParent[] Parents;

    public static ParseBuffers Rent() => new()
    {
        Frames = ArrayPool<ContainerFrame>.Shared.Rent(16),
        Records = ArrayPool<NodeRecord>.Shared.Rent(128),
        Parents = ArrayPool<OpenParent>.Shared.Rent(16),
    };

    public ref ContainerFrame Top => ref Frames[FrameCount - 1];

    public void Push(in ContainerFrame frame)
    {
        if (FrameCount == Frames.Length)
            Grow(ref Frames, FrameCount, clear: false);
        Frames[FrameCount++] = frame;
    }

    public ContainerFrame Pop() => Frames[--FrameCount];

    public int AddRecord(in NodeRecord record)
    {
        if (RecordCount == Records.Length)
            Grow(ref Records, RecordCount, clear: false);
        Records[RecordCount] = record;
        return RecordCount++;
    }

    public ref OpenParent ParentAt(int depth)
    {
        if (depth == Parents.Length)
            Grow(ref Parents, depth, clear: true);
        return ref Parents[depth];
    }

    /// <summary>
    /// Returns the buffers. Frames and records hold no references; the parent stack does, so it is cleared.
    /// </summary>
    public readonly void Return()
    {
        ArrayPool<ContainerFrame>.Shared.Return(Frames);
        ArrayPool<NodeRecord>.Shared.Return(Records);
        ArrayPool<OpenParent>.Shared.Return(Parents, clearArray: true);
    }

    private static void Grow<T>(ref T[] array, int used, bool clear)
    {
        var bigger = ArrayPool<T>.Shared.Rent(array.Length * 2);
        Array.Copy(array, bigger, used);
        ArrayPool<T>.Shared.Return(array, clear);
        array = bigger;
    }
}
