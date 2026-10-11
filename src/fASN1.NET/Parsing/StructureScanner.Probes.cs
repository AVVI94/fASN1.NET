using System;

namespace fASN1.NET.Parsing;

/// <summary>
/// The rules for content inside an encapsulation probe.
/// </summary>
/// <remarks>
/// A primitive OCTET STRING is tried as nested elements because X.509 wraps DER structures in OCTET STRINGs. If its
/// content is read as children, <see cref="Asn1Serializer.Serialize"/> writes those children back instead of the
/// bytes, so the children must encode to exactly the original bytes, or the OCTET STRING's value (often signed or
/// hashed) would change. Serialize writes DER, so everything inside a probe must already be DER in every respect
/// Serialize normalizes; otherwise the probe fails and the OCTET STRING stays a leaf with its raw bytes.
/// </remarks>
internal ref partial struct StructureScanner<TSource>
{
    /// <summary>
    /// Checks an element inside a probe against the DER rules on the encoding itself; on failure the probe rolls back.
    /// </summary>
    /// <param name="header">The element just read.</param>
    /// <param name="parentKind">The kind of the element containing it.</param>
    private bool CheckDerForm(in ElementHeader header, OpenElementKind parentKind)
    {
        // Lengths must be definite and minimal (X.690 §10.1).
        if (!header.HasDerLength)
            return FailProbe(ParseFailureKind.NonDerLengthInOctetString, header.Start);
        // An end-of-contents marker cannot be a value, so the content is not a run of elements.
        if (header.IsEndOfContents && parentKind == OpenElementKind.EncapsulationProbe)
            return FailProbe(ParseFailureKind.EndOfContentsInOctetString, header.Start);
        // DER has no constructed strings (§10.2); Serialize writes the joined value as one primitive string.
        if (Ber.StringSegmentTagNumber(header.Identifier) != 0)
            return FailProbe(ParseFailureKind.ConstructedStringInOctetString, header.Start);
        return true;
    }

    /// <summary>
    /// Checks the child of a SET inside a probe that just completed against the one before it: Serialize writes SET
    /// components in ascending order of their encodings (X.690 §11.6).
    /// </summary>
    private bool CheckDerSetOrder(ref OpenElement set)
    {
        if (set.ChildCount == 0 || _nodes[set.NodeIndex].Identifier != Ber.SET)
            return true;
        if (CompareEncodings(set.PreviousChildStart, set.ChildStart, set.ChildStart, _source.Position) <= 0)
            return true;
        return FailProbe(ParseFailureKind.SetNotInDerOrderInOctetString, set.ChildStart);
    }

    /// <summary>A primitive OCTET STRING whose content did not hold a complete run of elements: keep the raw bytes.</summary>
    private ElementScanResult RollBackProbe(in OpenElement probe)
    {
        _nodes.Truncate(probe.NodeIndex + 1);
        _nodes[probe.NodeIndex].IsRolledBackProbe = true;
        _source.Position = probe.ContentStart;
        return SkipContent(probe.Header);
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

    /// <summary>Records why the innermost probe fails; its message is only seen if the probe is not rolled back.</summary>
    private bool FailProbe(ParseFailureKind kind, long offset)
    {
        _failure = new ParseFailure(kind, offset);
        return false;
    }
}
