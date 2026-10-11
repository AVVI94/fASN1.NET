using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// Constructed strings (X.690 §8.6.4, §8.7.3, §8.23.6): a BIT STRING, OCTET STRING, character string or time split
/// into segments. The string becomes one leaf node whose content is the segments joined, recorded with its primitive
/// identifier, so the factory creates the same tag as for a primitive string. Segments get no nodes, only
/// <see cref="ContentSegment"/>s.
/// </summary>
internal ref partial struct StructureScanner<TSource>
{
    /// <summary>Starts a constructed string.</summary>
    private ElementScanResult OpenConstructedString(in ElementHeader header, int segmentTagNumber)
    {
        var nodeIndex = _nodes.Add(new ScannedNode
        {
            Identifier = header.Identifier & ~Ber.CONSTRUCTED_FLAG,
            TagNumber = header.TagNumber,
            ContentStart = header.ContentStart,
            IsJoinedString = true,
            FirstSegment = _segments.Count,
        });
        return Open(new OpenElement
        {
            Kind = OpenElementKind.ConstructedString,
            Header = header,
            NodeIndex = nodeIndex,
            SegmentTagNumber = segmentTagNumber,
        });
    }

    /// <summary>
    /// Reads one segment of the innermost constructed string or constructed segment: a primitive segment adds its
    /// content to the string, a constructed one is opened and its segments are added in turn.
    /// </summary>
    private ElementScanResult ScanStringSegment(in ElementHeader header)
    {
        ref var parent = ref _openElements.Last;
        var segmentTagNumber = parent.SegmentTagNumber;
        if ((header.Identifier & ~Ber.CONSTRUCTED_FLAG) != segmentTagNumber)
        {
            var expected = segmentTagNumber == (int)Tag.BitString ? "a BIT STRING" : "an OCTET STRING";
            return Fail(new ParseFailure(ParseFailureKind.InvalidStringSegment, header.Start, tagName: expected));
        }
        if (Ber.IsConstructed(header.Identifier))
        {
            return Open(new OpenElement
            {
                Kind = OpenElementKind.StringSegment,
                Header = header,
                NodeIndex = parent.NodeIndex,
                SegmentTagNumber = segmentTagNumber,
            });
        }

        var stringNodeIndex = parent.NodeIndex;
        var skipped = SkipContent(header);
        if (skipped != ElementScanResult.Succeeded)
            return skipped;

        var segment = new ContentSegment { Start = header.ContentStart, Length = header.ContentLength!.Value };
        if (segmentTagNumber == (int)Tag.BitString && !TryTakeUnusedBits(ref segment, header.Start, ref _nodes[stringNodeIndex]))
            return ElementScanResult.Failed;
        _segments.Add(segment);
        return ElementScanResult.Succeeded;
    }

    /// <summary>
    /// Each BIT STRING segment starts with its own unused-bits count, and only the last may be non-zero (§8.6.4).
    /// Moves the segment past that byte and records the count on the string's node.
    /// </summary>
    private bool TryTakeUnusedBits(ref ContentSegment segment, long segmentStart, ref ScannedNode bitString)
    {
        // A non-zero count on the previous segment means that one had to be the last.
        if (bitString.UnusedBits != 0 || segment.Length == 0)
            return FailBitStringSegment(segmentStart);

        var resume = _source.Position;
        _source.Position = segment.Start;
        bitString.UnusedBits = _source.ReadByte();
        _source.Position = resume;
        if (bitString.UnusedBits > 7 || (segment.Length == 1 && bitString.UnusedBits != 0))
            return FailBitStringSegment(segmentStart);

        segment.Start++;
        segment.Length--;
        return true;
    }

    /// <summary>Closes a constructed string: its content length is the total of its segments.</summary>
    private ElementScanResult JoinSegments(in OpenElement element)
    {
        ref var node = ref _nodes[element.NodeIndex];
        node.SegmentCount = _segments.Count - node.FirstSegment;
        long length = element.SegmentTagNumber == (int)Tag.BitString ? 1 : 0; // the unused-bits byte
        foreach (var segment in _segments.AsSpan().Slice(node.FirstSegment))
        {
            length += segment.Length;
        }
        if (length > int.MaxValue)
            return Fail(new ParseFailure(ParseFailureKind.LengthTooLarge, element.Header.Start));

        node.ContentLength = length;
        return ElementScanResult.Succeeded;
    }

    private bool FailBitStringSegment(long segmentStart)
    {
        _failure = new ParseFailure(ParseFailureKind.InvalidBitStringSegment, segmentStart);
        return false;
    }
}
