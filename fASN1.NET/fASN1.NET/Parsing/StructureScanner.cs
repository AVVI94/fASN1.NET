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
/// Elements whose children are being read are kept on an explicit stack of <see cref="OpenElement"/>s,
/// so nesting depth does not consume call stack space.
/// </para>
/// <para>
/// A primitive OCTET STRING is tried as child elements (an "encapsulation probe"), because X.509 wraps
/// DER structures in OCTET STRINGs. If that fails, the probe's nodes are truncated away and the element
/// becomes a leaf holding its raw bytes. Nothing was allocated for the discarded nodes.
/// </para>
/// </remarks>
internal ref struct StructureScanner<TSource>
    where TSource : struct, IByteSource
{
    private const int INITIAL_OPEN_ELEMENT_CAPACITY = 16;
    private const int INITIAL_NODE_CAPACITY = 128;

    private TSource _source;
    private readonly TagTraitsTable _traits;
    private PooledList<OpenElement> _openElements;
    private PooledList<ScannedNode> _nodes;
    private ParseFailure _failure;

    /// <summary>Offset of the identifier of the element that completed last.</summary>
    private long _completedElementStart;

    /// <param name="source">The input, positioned at the element to scan. The scanner reads a copy.</param>
    /// <param name="traits">Traits of the tags the factory creates.</param>
    public StructureScanner(TSource source, TagTraitsTable traits)
    {
        _source = source;
        _traits = traits;
        _openElements = new PooledList<OpenElement>(INITIAL_OPEN_ELEMENT_CAPACITY, clearOnRemove: false);
        _nodes = new PooledList<ScannedNode>(INITIAL_NODE_CAPACITY, clearOnRemove: false);
        _failure = default;
        _completedElementStart = 0;
    }

    /// <summary>The nodes of the final tree in pre-order, once <see cref="TryScan"/> has succeeded.</summary>
    public readonly ReadOnlySpan<ScannedNode> Nodes => _nodes.AsSpan();

    /// <summary>Why <see cref="TryScan"/> failed.</summary>
    public readonly ParseFailure Failure => _failure;

    /// <summary>Offset just past the scanned element, once <see cref="TryScan"/> has succeeded.</summary>
    public long Position => _source.Position;

    /// <summary>Scans the element at the current position and all of its descendants.</summary>
    public bool TryScan()
    {
        var result = ScanNextElement();
        while (true)
        {
            if (result == ElementScanResult.ChildrenOpened)
            {
                result = InnermostElementExpectsAnotherChild() ? ScanNextElement() : CloseCompletedInnermostElement();
                continue;
            }

            if (_openElements.Count == 0)
                return result == ElementScanResult.Succeeded;

            switch (result)
            {
                case ElementScanResult.EndOfContentsRead:
                    result = CloseElement(_openElements.RemoveLast(), childrenSucceeded: true);
                    break;

                case ElementScanResult.Failed:
                    result = CloseElement(_openElements.RemoveLast(), childrenSucceeded: false);
                    break;

                default:
                    // The element just completed is the next child of the innermost open element.
                    if (!IsInDerSetOrder())
                    {
                        result = Fail(new ParseFailure(ParseFailureKind.SetNotInDerOrderInOctetString, _completedElementStart));
                        break;
                    }
                    _openElements.Last.ChildCount++;
                    result = InnermostElementExpectsAnotherChild() ? ScanNextElement() : CloseCompletedInnermostElement();
                    break;
            }
        }
    }

    /// <summary>Returns the rented buffers.</summary>
    public void Dispose()
    {
        _openElements.Dispose();
        _nodes.Dispose();
    }

    /// <summary>
    /// Reads the identifier and length of the next element and records it as a node. A leaf is complete
    /// once its content has been skipped; an element read as children is opened.
    /// </summary>
    private ElementScanResult ScanNextElement()
    {
        var elementStart = _source.Position;
        var isInsideIndefiniteLength = _openElements.Count > 0 && _openElements.Last.IsIndefiniteLength;
        if (isInsideIndefiniteLength && elementStart >= CurrentLimit)
        {
            // The enclosing definite-length element ends here, so the end-of-contents marker is missing.
            return Fail(new ParseFailure(ParseFailureKind.MissingEndOfContents, _openElements.Last.ContentStart));
        }

        var identifier = _source.ReadByte();
        if (identifier == -1)
        {
            return Fail(isInsideIndefiniteLength
                ? new ParseFailure(ParseFailureKind.MissingEndOfContents, _openElements.Last.ContentStart)
                : new ParseFailure(ParseFailureKind.UnexpectedEndOfData, elementStart));
        }
        if ((identifier & Ber.TAG_NUMBER_MASK) == Ber.TAG_NUMBER_MASK)
        {
            // The tag number continues in the following bytes; reading them as the length would misplace every later element.
            return Fail(new ParseFailure(ParseFailureKind.MultiByteTagNumber, elementStart));
        }
        if (!TryReadLength(elementStart, out var contentLength, out var isDerLength))
            return ElementScanResult.Failed;
        if (_source.Position > CurrentLimit)
            return FailOutsideContainer();
        if (!isDerLength && _openElements.Count > 0 && _openElements.Last.RequiresDerLengths)
            return Fail(new ParseFailure(ParseFailureKind.NonDerLengthInOctetString, elementStart));

        if (identifier == Ber.END_OF_CONTENTS)
        {
            if (isInsideIndefiniteLength)
            {
                return contentLength == 0
                    ? ElementScanResult.EndOfContentsRead
                    : Fail(new ParseFailure(ParseFailureKind.InvalidEndOfContents, elementStart));
            }
            if (_openElements.Count > 0 && _openElements.Last.Kind == OpenElementKind.EncapsulationProbe)
                _openElements.Last.HasEndOfContentsChild = true;
        }

        var traits = _traits[identifier];
        var contentStart = _source.Position;
        var nodeIndex = _nodes.Add(new ScannedNode
        {
            Identifier = identifier,
            ContentStart = contentStart,
            ContentLength = contentLength ?? 0,
            IsIndefiniteLength = contentLength is null,
        });

        // Indefinite length is only valid for constructed elements, so such an OCTET STRING is not probed;
        // skipping its content fails below instead.
        if (traits.IsConstructed || (traits.IsEncapsulationCandidate && contentLength is not null))
        {
            return OpenChildren(new OpenElement
            {
                Kind = traits.IsConstructed ? OpenElementKind.Constructed : OpenElementKind.EncapsulationProbe,
                NodeIndex = nodeIndex,
                ElementStart = elementStart,
                ContentStart = contentStart,
                ContentLength = contentLength,
            });
        }

        _completedElementStart = elementStart;
        return SkipContent(traits.TagName, contentStart, contentLength);
    }

    /// <summary>Starts reading an element's content as children, unless its declared content cannot fit.</summary>
    private ElementScanResult OpenChildren(OpenElement element)
    {
        if (!element.IsIndefiniteLength && element.ContentEnd > _source.Length)
        {
            _failure = new ParseFailure(ParseFailureKind.ContainerPastEndOfData, element.ContentStart, element.ContentLength!.Value);
            return CloseElement(element, childrenSucceeded: false);
        }
        if (!element.IsIndefiniteLength && element.ContentEnd > CurrentLimit)
        {
            _failure = OutsideContainerFailure();
            return CloseElement(element, childrenSucceeded: false);
        }

        element.Limit = element.IsIndefiniteLength ? CurrentLimit : element.ContentEnd;
        element.LimitOwnerStart = element.IsIndefiniteLength ? CurrentLimitOwnerStart : element.ContentStart;
        element.RequiresDerLengths = element.Kind == OpenElementKind.EncapsulationProbe
            || (_openElements.Count > 0 && _openElements.Last.RequiresDerLengths);
        _openElements.Add(element);
        return ElementScanResult.ChildrenOpened;
    }

    private bool InnermostElementExpectsAnotherChild()
    {
        ref var innermost = ref _openElements.Last;
        return innermost.IsIndefiniteLength || _source.Position < innermost.ContentEnd;
    }

    /// <summary>Closes a definite-length element whose content has been read up to (or past) its end.</summary>
    private ElementScanResult CloseCompletedInnermostElement()
    {
        var element = _openElements.RemoveLast();
        var endsExactly = _source.Position == element.ContentEnd;
        if (!endsExactly)
            _failure = new ParseFailure(ParseFailureKind.ContainerSizeMismatch, element.ContentStart);
        return CloseElement(element, childrenSucceeded: endsExactly);
    }

    /// <summary>
    /// Records the outcome of reading an element's children. A constructed element fails with its
    /// children; a probe that did not hold a complete run of children becomes a leaf with raw content.
    /// </summary>
    private ElementScanResult CloseElement(in OpenElement element, bool childrenSucceeded)
    {
        _completedElementStart = element.ElementStart;
        ref var node = ref _nodes[element.NodeIndex];
        var keepsChildren = element.Kind == OpenElementKind.Constructed
            ? childrenSucceeded
            : childrenSucceeded && !element.HasEndOfContentsChild;

        if (keepsChildren)
        {
            node.ContentIsChildren = true;
            node.ChildCount = element.ChildCount;
            return ElementScanResult.Succeeded;
        }
        if (element.Kind == OpenElementKind.Constructed)
            return ElementScanResult.Failed;

        _nodes.Truncate(element.NodeIndex + 1);
        node.IsRolledBackProbe = true;
        _source.Position = element.ContentStart;
        return SkipContent(_traits[node.Identifier].TagName, element.ContentStart, element.ContentLength);
    }

    /// <summary>Checks that a leaf's content is present and moves past it.</summary>
    private ElementScanResult SkipContent(string tagName, long contentStart, long? contentLength)
    {
        if (contentLength is null)
            return Fail(new ParseFailure(ParseFailureKind.IndefiniteLengthPrimitive, contentStart));
        if (contentLength.Value > _source.Length - _source.Position)
            return Fail(new ParseFailure(ParseFailureKind.ContentPastEndOfData, contentStart, tagName: tagName));
        if (_source.Position + contentLength.Value > CurrentLimit)
            return FailOutsideContainer();

        _source.Position += contentLength.Value;
        return ElementScanResult.Succeeded;
    }

    /// <summary>Reads the length bytes that follow an identifier.</summary>
    /// <param name="elementStart">Offset of the identifier, for error messages.</param>
    /// <param name="contentLength">The content length, or <see langword="null"/> for indefinite length.</param>
    /// <param name="isDer">The length is definite and in the shortest form, as DER requires.</param>
    private bool TryReadLength(long elementStart, out long? contentLength, out bool isDer)
    {
        contentLength = null;
        isDer = false;
        var first = _source.ReadByte();
        if (first == -1)
            return FailLength(new ParseFailure(ParseFailureKind.LengthTruncated, elementStart));

        if ((first & Ber.LONG_FORM_LENGTH_FLAG) == 0)
        {
            contentLength = first; // short form: the byte is the length
            isDer = true;
            return true;
        }

        var octetCount = first & Ber.LENGTH_OCTET_COUNT_MASK;
        if (octetCount == 0)
            return true; // indefinite length
        if (octetCount > Ber.MAX_LENGTH_OCTETS)
            return FailLength(new ParseFailure(ParseFailureKind.TooManyLengthOctets, elementStart, octetCount));

        long length = 0;
        for (int i = 0; i < octetCount; i++)
        {
            var octet = _source.ReadByte();
            if (octet == -1)
                return FailLength(new ParseFailure(ParseFailureKind.LengthTruncated, elementStart));
            if (i == 0)
                isDer = octet != 0; // a leading zero octet means fewer octets would do
            length = (length << 8) | (uint)octet;
        }
        isDer &= length >= 0x80; // shorter lengths must use the short form

        // Content is read into a byte[], so lengths are limited to what an array can index.
        if (length > int.MaxValue)
            return FailLength(new ParseFailure(ParseFailureKind.LengthTooLarge, elementStart));

        contentLength = length;
        return true;
    }

    /// <summary>
    /// Checks the element that just completed against the previous child when the innermost open element is a SET
    /// inside an encapsulation probe: Serialize writes SET children in DER order (X.690 §11.6), so children in any
    /// other order would change the OCTET STRING's value.
    /// </summary>
    private bool IsInDerSetOrder()
    {
        ref var parent = ref _openElements.Last;
        if (!parent.RequiresDerLengths || _nodes[parent.NodeIndex].Identifier != Ber.SET)
            return true;

        var end = _source.Position;
        if (parent.ChildCount > 0
            && CompareEncodings(parent.PreviousChildStart, parent.PreviousChildEnd, _completedElementStart, end) > 0)
        {
            return false;
        }
        parent.PreviousChildStart = _completedElementStart;
        parent.PreviousChildEnd = end;
        return true;
    }

    /// <summary>
    /// Compares two encodings in the input as octet strings, the shorter one padded with zero bytes. Reads byte by
    /// byte without allocating; the order is almost always settled by the first bytes.
    /// </summary>
    private int CompareEncodings(long aStart, long aEnd, long bStart, long bEnd)
    {
        var resume = _source.Position;
        long aLength = aEnd - aStart, bLength = bEnd - bStart, common = Math.Min(aLength, bLength);
        var result = 0;
        for (long i = 0; i < common && result == 0; i++)
        {
            _source.Position = aStart + i;
            var a = _source.ReadByte();
            _source.Position = bStart + i;
            result = a - _source.ReadByte();
        }
        if (result == 0 && aLength != bLength)
        {
            // The longer one is greater exactly when its remaining bytes are not all padding zeros.
            var (longerStart, longerLength, sign) = aLength > bLength ? (aStart, aLength, 1) : (bStart, bLength, -1);
            _source.Position = longerStart + common;
            for (long i = common; i < longerLength && result == 0; i++)
            {
                if (_source.ReadByte() != 0)
                    result = sign;
            }
        }
        _source.Position = resume;
        return result;
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

    private bool FailLength(ParseFailure failure)
    {
        _failure = failure;
        return false;
    }
}
