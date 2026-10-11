using System;

namespace fASN1.NET.Parsing;

/// <summary>
/// First pass of the parser: reads every identifier and length, validates the structure and decides the
/// shape of the final tree, without creating any tag.
/// </summary>
/// <remarks>
/// <para>
/// The result is the list of <see cref="Nodes"/> in pre-order. Content bytes are skipped, not read;
/// <see cref="TreeBuilder"/> reads the ones that end up in the tree.
/// </para>
/// <para>
/// Elements whose content is being read as nested elements are kept on an explicit stack of
/// <see cref="OpenElement"/>s, so nesting depth does not consume call stack space. Every step of the scan is one
/// of: read the next element, add a finished element to its parent, or close the innermost open element.
/// </para>
/// <para>
/// Open elements come in three kinds (<see cref="OpenElementKind"/>): constructed elements; primitive OCTET STRINGs
/// tried as nested elements ("encapsulation probes", <c>StructureScanner.Probes.cs</c>), which roll back to raw bytes
/// if that fails; and constructed strings, whose segments are joined (<c>StructureScanner.Strings.cs</c>).
/// </para>
/// </remarks>
internal ref partial struct StructureScanner<TSource>
    where TSource : struct, IByteSource
{
    private const int INITIAL_OPEN_ELEMENT_CAPACITY = 16;
    private const int INITIAL_NODE_CAPACITY = 128;
    private const int INITIAL_SEGMENT_CAPACITY = 16;

    private TSource _source;
    private PooledList<OpenElement> _openElements;
    private PooledList<ScannedNode> _nodes;
    private PooledList<ContentSegment> _segments;
    private ParseFailure _failure;

    /// <param name="source">The input, positioned at the element to scan. The scanner reads a copy.</param>
    public StructureScanner(TSource source)
    {
        _source = source;
        _openElements = new PooledList<OpenElement>(INITIAL_OPEN_ELEMENT_CAPACITY, clearOnRemove: false);
        _nodes = new PooledList<ScannedNode>(INITIAL_NODE_CAPACITY, clearOnRemove: false);
        _segments = new PooledList<ContentSegment>(INITIAL_SEGMENT_CAPACITY, clearOnRemove: false);
        _failure = default;
    }

    /// <summary>The nodes of the final tree in pre-order, once <see cref="TryScan"/> has succeeded.</summary>
    public readonly ReadOnlySpan<ScannedNode> Nodes => _nodes.AsSpan();

    /// <summary>The segments of the joined strings among <see cref="Nodes"/>.</summary>
    public readonly ReadOnlySpan<ContentSegment> Segments => _segments.AsSpan();

    /// <summary>Why <see cref="TryScan"/> failed.</summary>
    public readonly ParseFailure Failure => _failure;

    /// <summary>Offset just past the scanned element, once <see cref="TryScan"/> has succeeded.</summary>
    public readonly long Position => _source.Position;

    /// <summary>Scans the element at the current position and all of its descendants.</summary>
    public bool TryScan()
    {
        var result = ScanElement();
        while (_openElements.Count > 0 || result == ElementScanResult.ChildrenOpened)
        {
            result = result switch
            {
                ElementScanResult.ChildrenOpened => ContinueInnermostElement(),
                ElementScanResult.Succeeded => AddChildToInnermostElement(),
                ElementScanResult.EndOfContentsRead => CloseElement(_openElements.RemoveLast(), childrenSucceeded: true),
                _ => CloseElement(_openElements.RemoveLast(), childrenSucceeded: false),
            };
        }
        return result == ElementScanResult.Succeeded;
    }

    /// <summary>Returns the rented buffers.</summary>
    public void Dispose()
    {
        _openElements.Dispose();
        _nodes.Dispose();
        _segments.Dispose();
    }

    /// <summary>
    /// Reads the next element. A leaf is complete once its content has been skipped; an element whose content is
    /// read as nested elements is opened.
    /// </summary>
    private ElementScanResult ScanElement()
    {
        var start = _source.Position;
        var insideIndefiniteLength = false;
        if (_openElements.Count > 0)
        {
            ref var parent = ref _openElements.Last;
            parent.ChildStart = start;
            insideIndefiniteLength = parent.IsIndefiniteLength;
            // The enclosing definite-length element ends here, so the end-of-contents marker is missing.
            if (insideIndefiniteLength && start >= parent.Limit)
                return Fail(new ParseFailure(ParseFailureKind.MissingEndOfContents, parent.ContentStart));
        }

        if (!ElementHeaderReader.TryRead(ref _source, out var header, out var failure))
        {
            return Fail(insideIndefiniteLength && failure.Kind == ParseFailureKind.UnexpectedEndOfData
                ? new ParseFailure(ParseFailureKind.MissingEndOfContents, _openElements.Last.ContentStart)
                : failure);
        }
        if (_source.Position > CurrentLimit)
            return FailOutsideContainer();

        if (header.IsEndOfContents && insideIndefiniteLength)
        {
            return header.ContentLength == 0
                ? ElementScanResult.EndOfContentsRead
                : Fail(new ParseFailure(ParseFailureKind.InvalidEndOfContents, header.Start));
        }
        if (_openElements.Count > 0)
        {
            ref var parent = ref _openElements.Last;
            if (parent.RequiresDer && !CheckDerForm(header, parent.Kind))
                return ElementScanResult.Failed;
            if (parent.Kind is OpenElementKind.ConstructedString or OpenElementKind.StringSegment)
                return ScanStringSegment(header);
        }

        var segmentTagNumber = Ber.StringSegmentTagNumber(header.Identifier);
        if (segmentTagNumber != 0)
            return OpenConstructedString(header, segmentTagNumber);

        var nodeIndex = _nodes.Add(new ScannedNode
        {
            Identifier = header.Identifier,
            TagNumber = header.TagNumber,
            ContentStart = header.ContentStart,
            ContentLength = header.ContentLength ?? 0,
            IsIndefiniteLength = header.IsIndefiniteLength,
        });
        if (Ber.IsConstructed(header.Identifier))
            return Open(new OpenElement { Kind = OpenElementKind.Constructed, Header = header, NodeIndex = nodeIndex });
        // Indefinite length is only valid for constructed elements, so such an OCTET STRING is not probed;
        // skipping its content fails instead.
        if (Ber.IsEncapsulationCandidate(header.Identifier) && !header.IsIndefiniteLength)
            return Open(new OpenElement { Kind = OpenElementKind.EncapsulationProbe, Header = header, NodeIndex = nodeIndex });
        return SkipContent(header);
    }

    /// <summary>Starts reading an element's content as nested elements, unless its declared content cannot fit.</summary>
    private ElementScanResult Open(OpenElement element)
    {
        if (!element.IsIndefiniteLength)
        {
            if (element.ContentEnd > _source.Length)
            {
                _failure = new ParseFailure(ParseFailureKind.ContainerPastEndOfData, element.ContentStart, element.Header.ContentLength!.Value);
                return CloseElement(element, childrenSucceeded: false);
            }
            if (element.ContentEnd > CurrentLimit)
            {
                _failure = OutsideContainerFailure();
                return CloseElement(element, childrenSucceeded: false);
            }
        }

        element.Limit = element.IsIndefiniteLength ? CurrentLimit : element.ContentEnd;
        element.LimitOwnerStart = element.IsIndefiniteLength ? CurrentLimitOwnerStart : element.ContentStart;
        element.RequiresDer = element.Kind == OpenElementKind.EncapsulationProbe
            || (_openElements.Count > 0 && _openElements.Last.RequiresDer);
        _openElements.Add(element);
        return ElementScanResult.ChildrenOpened;
    }

    /// <summary>The element just completed is the next child of the innermost open element.</summary>
    private ElementScanResult AddChildToInnermostElement()
    {
        ref var parent = ref _openElements.Last;
        if (parent.RequiresDer && !CheckDerSetOrder(ref parent))
            return ElementScanResult.Failed;

        parent.PreviousChildStart = parent.ChildStart;
        parent.ChildCount++;
        return ContinueInnermostElement();
    }

    /// <summary>Reads the next child of the innermost open element, or closes it when its content is used up.</summary>
    private ElementScanResult ContinueInnermostElement()
    {
        ref var innermost = ref _openElements.Last;
        if (innermost.IsIndefiniteLength || _source.Position < innermost.ContentEnd)
            return ScanElement();

        var element = _openElements.RemoveLast();
        var endsExactly = _source.Position == element.ContentEnd;
        if (!endsExactly)
            _failure = new ParseFailure(ParseFailureKind.ContainerSizeMismatch, element.ContentStart);
        return CloseElement(element, childrenSucceeded: endsExactly);
    }

    /// <summary>
    /// Records the outcome of reading an element's content as nested elements. A constructed element or string fails
    /// with its children; a probe whose content is not a complete run of elements becomes a leaf with raw content.
    /// </summary>
    private ElementScanResult CloseElement(in OpenElement element, bool childrenSucceeded)
    {
        switch (element.Kind)
        {
            case OpenElementKind.Constructed:
                return childrenSucceeded ? KeepChildren(element) : ElementScanResult.Failed;
            case OpenElementKind.EncapsulationProbe:
                return childrenSucceeded ? KeepChildren(element) : RollBackProbe(element);
            case OpenElementKind.ConstructedString:
                return childrenSucceeded ? JoinSegments(element) : ElementScanResult.Failed;
            default: // a constructed segment: its segments already belong to its string
                return childrenSucceeded ? ElementScanResult.Succeeded : ElementScanResult.Failed;
        }
    }

    private ElementScanResult KeepChildren(in OpenElement element)
    {
        ref var node = ref _nodes[element.NodeIndex];
        node.ContentIsChildren = true;
        node.ChildCount = element.ChildCount;
        return ElementScanResult.Succeeded;
    }

    /// <summary>Checks that a leaf's content is present and moves past it.</summary>
    private ElementScanResult SkipContent(in ElementHeader header)
    {
        if (header.ContentLength is not { } length)
            return Fail(new ParseFailure(ParseFailureKind.IndefiniteLengthPrimitive, header.ContentStart));
        if (length > _source.Length - _source.Position)
            return Fail(new ParseFailure(ParseFailureKind.ContentPastEndOfData, header.ContentStart, identifier: header.Identifier, tagNumber: header.TagNumber));
        if (_source.Position + length > CurrentLimit)
            return FailOutsideContainer();

        _source.Position += length;
        return ElementScanResult.Succeeded;
    }

    /// <summary>Offset that the element being scanned may not cross; see <see cref="OpenElement.Limit"/>.</summary>
    private readonly long CurrentLimit => _openElements.Count > 0 ? _openElements.Last.Limit : _source.Length;

    private readonly long CurrentLimitOwnerStart => _openElements.Count > 0 ? _openElements.Last.LimitOwnerStart : 0;

    /// <summary>The element being scanned crosses the end of a definite-length element that contains it.</summary>
    private readonly ParseFailure OutsideContainerFailure() => new(ParseFailureKind.ContainerSizeMismatch, CurrentLimitOwnerStart);

    private ElementScanResult FailOutsideContainer() => Fail(OutsideContainerFailure());

    private ElementScanResult Fail(ParseFailure failure)
    {
        _failure = failure;
        return ElementScanResult.Failed;
    }
}
