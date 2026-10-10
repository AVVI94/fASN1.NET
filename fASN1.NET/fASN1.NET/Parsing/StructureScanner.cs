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

    /// <param name="source">The input, positioned at the element to scan. The scanner reads a copy.</param>
    /// <param name="traits">Traits of the tags the factory creates.</param>
    public StructureScanner(TSource source, TagTraitsTable traits)
    {
        _source = source;
        _traits = traits;
        _openElements = new PooledList<OpenElement>(INITIAL_OPEN_ELEMENT_CAPACITY, clearOnRemove: false);
        _nodes = new PooledList<ScannedNode>(INITIAL_NODE_CAPACITY, clearOnRemove: false);
        _failure = default;
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
        var identifier = _source.ReadByte();
        var isInsideIndefiniteLength = _openElements.Count > 0 && _openElements.Last.IsIndefiniteLength;

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
        if (!TryReadLength(elementStart, out var contentLength))
            return ElementScanResult.Failed;

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
                ContentStart = contentStart,
                ContentLength = contentLength,
            });
        }

        return SkipContent(traits.TagName, contentStart, contentLength);
    }

    /// <summary>Starts reading an element's content as children, unless its declared content cannot fit.</summary>
    private ElementScanResult OpenChildren(in OpenElement element)
    {
        if (!element.IsIndefiniteLength && element.ContentEnd > _source.Length)
        {
            _failure = new ParseFailure(ParseFailureKind.ContainerPastEndOfData, element.ContentStart, element.ContentLength!.Value);
            return CloseElement(element, childrenSucceeded: false);
        }
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

        _source.Position += contentLength.Value;
        return ElementScanResult.Succeeded;
    }

    /// <summary>Reads the length bytes that follow an identifier.</summary>
    /// <param name="elementStart">Offset of the identifier, for error messages.</param>
    /// <param name="contentLength">The content length, or <see langword="null"/> for indefinite length.</param>
    private bool TryReadLength(long elementStart, out long? contentLength)
    {
        contentLength = null;
        var first = _source.ReadByte();
        if (first == -1)
            return FailLength(new ParseFailure(ParseFailureKind.LengthTruncated, elementStart));

        if ((first & Ber.LONG_FORM_LENGTH_FLAG) == 0)
        {
            contentLength = first; // short form: the byte is the length
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
            length = (length << 8) | (uint)octet;
        }

        // Content is read into a byte[], so lengths are limited to what an array can index.
        if (length > int.MaxValue)
            return FailLength(new ParseFailure(ParseFailureKind.LengthTooLarge, elementStart));

        contentLength = length;
        return true;
    }

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
