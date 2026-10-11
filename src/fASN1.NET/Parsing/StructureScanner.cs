using System;

namespace fASN1.NET.Parsing;

/// <summary>
/// First pass of the parser: reads the identifier and length octets of every encoding, validates the structure and
/// decides the shape of the final tree, without creating any tag.
/// </summary>
/// <remarks>
/// <para>
/// The result is the list of <see cref="Nodes"/> in pre-order. Contents octets of primitive encodings are skipped, not
/// read; <see cref="TreeBuilder"/> reads the ones that end up in the tree.
/// </para>
/// <para>
/// Encodings whose contents are being read as nested encodings are kept on an explicit stack of
/// <see cref="EnclosingEncoding"/>s, so nesting depth does not consume call stack space. Every step of the scan is one
/// of: read the next encoding, add a complete encoding to the one enclosing it, or leave the contents of the innermost
/// enclosing encoding.
/// </para>
/// <para>
/// Contents are read as nested encodings for three reasons (<see cref="EnclosingEncodingKind"/>): the encoding is
/// constructed; it is a primitive OCTET STRING that may encapsulate DER encodings
/// (<c>StructureScanner.Encapsulation.cs</c>), kept as raw contents if it does not; or it is a string in the
/// constructed encoding, whose fragments are joined (<c>StructureScanner.ConstructedStrings.cs</c>).
/// </para>
/// </remarks>
/// <param name="source">The input, positioned at the encoding to scan. The scanner reads a copy.</param>
internal ref partial struct StructureScanner<TSource>(TSource source)
    where TSource : struct, IByteSource
{
    private const int INITIAL_ENCLOSING_CAPACITY = 16;
    private const int INITIAL_NODE_CAPACITY = 128;
    private const int INITIAL_FRAGMENT_CAPACITY = 16;

    private TSource _source = source;
    private PooledList<EnclosingEncoding> _enclosing = new(INITIAL_ENCLOSING_CAPACITY, clearOnRemove: false);
    private PooledList<ScannedNode> _nodes = new(INITIAL_NODE_CAPACITY, clearOnRemove: false);
    private PooledList<ContentsFragment> _fragments = new(INITIAL_FRAGMENT_CAPACITY, clearOnRemove: false);
    private ParseFailure _failure = default;

    /// <summary>Offset the encoding being scanned may not cross; see <see cref="EnclosingEncoding.ContentsBound"/>.</summary>
    private readonly long CurrentContentsBound => _enclosing.Count > 0 ? _enclosing.Last.ContentsBound : _source.Length;

    private readonly long CurrentContentsBoundStart => _enclosing.Count > 0 ? _enclosing.Last.ContentsBoundStart : 0;

    /// <summary>The nodes of the final tree in pre-order, once <see cref="TryScan"/> has succeeded.</summary>
    public readonly ReadOnlySpan<ScannedNode> Nodes => _nodes.AsSpan();

    /// <summary>The fragments of the constructed strings among <see cref="Nodes"/>.</summary>
    public readonly ReadOnlySpan<ContentsFragment> Fragments => _fragments.AsSpan();

    /// <summary>Why <see cref="TryScan"/> failed.</summary>
    public readonly ParseFailure Failure => _failure;

    /// <summary>Offset just past the scanned encoding, once <see cref="TryScan"/> has succeeded.</summary>
    public readonly long Position => _source.Position;

    /// <summary>Scans the encoding at the current position and all the encodings nested in it.</summary>
    public bool TryScan()
    {
        var result = ScanEncoding();
        while (_enclosing.Count > 0 || result == ScanResult.ContentsEntered)
        {
            result = result switch
            {
                ScanResult.ContentsEntered => ContinueInnermostContents(),
                ScanResult.Succeeded => AddNestedEncoding(),
                ScanResult.EndOfContentsRead => LeaveContents(_enclosing.RemoveLast(), nestedEncodingsValid: true),
                _ => LeaveContents(_enclosing.RemoveLast(), nestedEncodingsValid: false),
            };
        }
        return result == ScanResult.Succeeded;
    }

    /// <summary>Returns the rented buffers.</summary>
    public void Dispose()
    {
        _enclosing.Dispose();
        _nodes.Dispose();
        _fragments.Dispose();
    }

    /// <summary>
    /// Reads the next encoding. A primitive encoding is complete once its contents have been skipped; an encoding
    /// whose contents are read as nested encodings is entered.
    /// </summary>
    private ScanResult ScanEncoding()
    {
        var start = _source.Position;
        var insideIndefiniteForm = false;
        if (_enclosing.Count > 0)
        {
            ref var enclosing = ref _enclosing.Last;
            enclosing.NestedEncodingStart = start;
            insideIndefiniteForm = enclosing.IsIndefiniteForm;
            // A definite-form ancestor's contents end here, so the end-of-contents octets are missing.
            if (insideIndefiniteForm && start >= enclosing.ContentsBound)
            {
                return Fail(new ParseFailure(ParseFailureKind.MissingEndOfContents, enclosing.ContentsStart));
            }
        }

        if (!IdentifierAndLengthReader.TryRead(ref _source, out var encoding, out var failure))
        {
            return Fail(insideIndefiniteForm && failure.Kind == ParseFailureKind.UnexpectedEndOfData
                ? new ParseFailure(ParseFailureKind.MissingEndOfContents, _enclosing.Last.ContentsStart)
                : failure);
        }
        if (_source.Position > CurrentContentsBound)
        {
            return FailCrossingContentsBound();
        }

        if (encoding.IsEndOfContentsOctets && insideIndefiniteForm)
        {
            return encoding.ContentsLength == 0
                ? ScanResult.EndOfContentsRead
                : Fail(new ParseFailure(ParseFailureKind.InvalidEndOfContents, encoding.IdentifierStart));
        }
        if (_enclosing.Count > 0)
        {
            ref var enclosing = ref _enclosing.Last;
            if (enclosing.MustBeDer && !CheckDerForm(encoding, enclosing.Kind))
            {
                return ScanResult.Failed;
            }

            if (enclosing.Kind is EnclosingEncodingKind.ConstructedString or EnclosingEncodingKind.ConstructedFragment)
            {
                return ScanFragment(encoding);
            }
        }

        var fragmentTagNumber = Ber.FragmentTagNumber(encoding.IdentifierOctet);
        if (fragmentTagNumber != 0)
        {
            return EnterConstructedString(encoding, fragmentTagNumber);
        }

        var nodeIndex = _nodes.Add(new ScannedNode
        {
            IdentifierOctet = encoding.IdentifierOctet,
            TagNumber = encoding.TagNumber,
            ContentsStart = encoding.ContentsStart,
            ContentsLength = encoding.ContentsLength ?? 0,
            IsIndefiniteForm = encoding.IsIndefiniteForm,
        });
        if (Ber.IsConstructed(encoding.IdentifierOctet))
        {
            return EnterContents(new EnclosingEncoding { Kind = EnclosingEncodingKind.Constructed, IdentifierAndLength = encoding, NodeIndex = nodeIndex });
        }
        // The indefinite form is valid only for constructed encodings, so such an OCTET STRING is not entered;
        // skipping its contents fails instead.
        if (Ber.MayEncapsulate(encoding.IdentifierOctet) && !encoding.IsIndefiniteForm)
        {
            return EnterContents(new EnclosingEncoding { Kind = EnclosingEncodingKind.EncapsulatingOctetString, IdentifierAndLength = encoding, NodeIndex = nodeIndex });
        }

        return SkipContents(encoding);
    }

    /// <summary>Starts reading an encoding's contents as nested encodings, unless its declared contents cannot fit.</summary>
    private ScanResult EnterContents(EnclosingEncoding enclosing)
    {
        if (!enclosing.IsIndefiniteForm)
        {
            if (enclosing.ContentsEnd > _source.Length)
            {
                _failure = new ParseFailure(ParseFailureKind.NestedContentsPastEndOfData, enclosing.ContentsStart, enclosing.IdentifierAndLength.ContentsLength!.Value);
                return LeaveContents(enclosing, nestedEncodingsValid: false);
            }
            if (enclosing.ContentsEnd > CurrentContentsBound)
            {
                _failure = CrossingContentsBoundFailure();
                return LeaveContents(enclosing, nestedEncodingsValid: false);
            }
        }

        enclosing.ContentsBound = enclosing.IsIndefiniteForm ? CurrentContentsBound : enclosing.ContentsEnd;
        enclosing.ContentsBoundStart = enclosing.IsIndefiniteForm ? CurrentContentsBoundStart : enclosing.ContentsStart;
        enclosing.MustBeDer = enclosing.Kind == EnclosingEncodingKind.EncapsulatingOctetString || (_enclosing.Count > 0 && _enclosing.Last.MustBeDer);
        _enclosing.Add(enclosing);
        return ScanResult.ContentsEntered;
    }

    /// <summary>The encoding just completed is the next nested encoding of the innermost enclosing encoding.</summary>
    private ScanResult AddNestedEncoding()
    {
        ref var enclosing = ref _enclosing.Last;
        if (enclosing.MustBeDer && !CheckDerSetOrder(ref enclosing))
        {
            return ScanResult.Failed;
        }

        enclosing.PreviousNestedEncodingStart = enclosing.NestedEncodingStart;
        enclosing.NestedEncodingCount++;
        return ContinueInnermostContents();
    }

    /// <summary>
    /// Reads the next nested encoding of the innermost enclosing encoding, or leaves its contents when they are used up.
    /// </summary>
    private ScanResult ContinueInnermostContents()
    {
        ref var innermost = ref _enclosing.Last;
        if (innermost.IsIndefiniteForm || _source.Position < innermost.ContentsEnd)
        {
            return ScanEncoding();
        }

        var enclosing = _enclosing.RemoveLast();
        var endsExactly = _source.Position == enclosing.ContentsEnd;
        if (!endsExactly)
        {
            _failure = new ParseFailure(ParseFailureKind.ContentsLengthMismatch, enclosing.ContentsStart);
        }

        return LeaveContents(enclosing, nestedEncodingsValid: endsExactly);
    }

    /// <summary>
    /// Records the outcome of reading an encoding's contents as nested encodings. A constructed encoding or string is
    /// invalid with them; an OCTET STRING whose contents are not a complete run of encodings becomes a leaf with its
    /// raw contents.
    /// </summary>
    private ScanResult LeaveContents(in EnclosingEncoding enclosing, bool nestedEncodingsValid) => enclosing.Kind switch
    {
        EnclosingEncodingKind.Constructed => nestedEncodingsValid ? KeepNestedEncodings(enclosing) : ScanResult.Failed,
        EnclosingEncodingKind.EncapsulatingOctetString => nestedEncodingsValid ? KeepNestedEncodings(enclosing) : RejectEncapsulation(enclosing),
        EnclosingEncodingKind.ConstructedString => nestedEncodingsValid ? JoinFragments(enclosing) : ScanResult.Failed,
        // a constructed fragment: its fragments already belong to its string
        _ => nestedEncodingsValid ? ScanResult.Succeeded : ScanResult.Failed,
    };

    /// <summary>The nested encodings become the node's children.</summary>
    private ScanResult KeepNestedEncodings(in EnclosingEncoding enclosing)
    {
        ref var node = ref _nodes[enclosing.NodeIndex];
        node.ContentsAreChildren = true;
        node.ChildCount = enclosing.NestedEncodingCount;
        return ScanResult.Succeeded;
    }

    /// <summary>Checks that the contents octets of a primitive encoding are present and moves past them.</summary>
    private ScanResult SkipContents(in IdentifierAndLength encoding)
    {
        if (encoding.ContentsLength is not { } length)
        {
            return Fail(new ParseFailure(ParseFailureKind.IndefiniteFormInPrimitive, encoding.ContentsStart));
        }

        if (length > _source.Length - _source.Position)
        {
            return Fail(new ParseFailure(ParseFailureKind.ContentsPastEndOfData, encoding.ContentsStart, identifierOctet: encoding.IdentifierOctet, tagNumber: encoding.TagNumber));
        }

        if (_source.Position + length > CurrentContentsBound)
        {
            return FailCrossingContentsBound();
        }

        _source.Position += length;
        return ScanResult.Succeeded;
    }

    /// <summary>The encoding being scanned crosses the end of the contents of a definite-form encoding enclosing it.</summary>
    private readonly ParseFailure CrossingContentsBoundFailure() => new(ParseFailureKind.ContentsLengthMismatch, CurrentContentsBoundStart);

    private ScanResult FailCrossingContentsBound() => Fail(CrossingContentsBoundFailure());

    private ScanResult Fail(ParseFailure failure)
    {
        _failure = failure;
        return ScanResult.Failed;
    }
}
