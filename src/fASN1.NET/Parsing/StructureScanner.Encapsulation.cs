using System;

namespace fASN1.NET.Parsing;

/// <summary>
/// Primitive OCTET STRINGs that encapsulate encodings.
/// </summary>
/// <remarks>
/// X.509 nests DER encodings in OCTET STRINGs (an extension's extnValue, for example), so the contents of a primitive
/// OCTET STRING are tried as encodings. If they are read as encodings, they become the node's children and
/// <see cref="Asn1Serializer.Serialize"/> re-encodes them instead of writing the contents octets. They must re-encode to
/// exactly the original octets, or the OCTET STRING's value (often signed or hashed) would change. Serialize writes
/// DER, so every encoding inside must already be DER in each respect Serialize normalizes; otherwise the encapsulation
/// is rejected and the OCTET STRING stays a leaf with its raw contents octets.
/// </remarks>
internal ref partial struct StructureScanner<TSource>
{
    /// <summary>
    /// Checks an encoding inside an encapsulating OCTET STRING against the DER rules on the form of the encoding;
    /// on failure the encapsulation is rejected.
    /// </summary>
    /// <param name="encoding">The identifier and length octets just read.</param>
    private bool CheckDerForm(in IdentifierAndLength encoding)
    {
        // The length must be in the definite form, with the fewest octets (X.690 §10.1).
        if (!encoding.IsShortestDefiniteForm)
        {
            return FailEncapsulation(ParseFailureKind.NonDerLengthInEncapsulation, encoding.IdentifierStart);
        }
        // DER encodes strings only in the primitive form (§10.2); Serialize writes a constructed string as one.
        if (Ber.FragmentTagNumber(encoding.IdentifierOctet) != 0)
        {
            return FailEncapsulation(ParseFailureKind.ConstructedStringInEncapsulation, encoding.IdentifierStart);
        }

        return true;
    }

    /// <summary>
    /// Checks a component of a SET inside an encapsulating OCTET STRING, once complete, against the component before
    /// it: Serialize writes SET components in ascending order of their encodings (X.690 §11.6).
    /// </summary>
    private bool CheckDerSetOrder(ref EnclosingEncoding set)
    {
        if (set.NestedEncodingCount == 0 || _nodes[set.NodeIndex].IdentifierOctet != Ber.SET)
        {
            return true;
        }

        if (CompareEncodings(set.PreviousNestedEncodingStart, set.NestedEncodingStart, set.NestedEncodingStart, _source.Position) <= 0)
        {
            return true;
        }

        return FailEncapsulation(ParseFailureKind.SetNotInDerOrderInEncapsulation, set.NestedEncodingStart);
    }

    /// <summary>The contents of a primitive OCTET STRING are not a run of DER encodings: keep the contents octets.</summary>
    private ScanResult RejectEncapsulation(in EnclosingEncoding octetString)
    {
        _nodes.Truncate(octetString.NodeIndex + 1);
        _nodes[octetString.NodeIndex].IsEncapsulationRejected = true;
        _source.Position = octetString.ContentsStart;
        return SkipContents(octetString.IdentifierAndLength);
    }

    /// <summary>
    /// Compares two encodings in the input as octet strings, the shorter one padded with zero octets (X.690 §11.6).
    /// Reads octet by octet without allocating; the order is almost always settled by the first octets.
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
            // The longer one is greater exactly when its remaining octets are not all padding zeros.
            var (longerStart, longerLength, sign) = aLength > bLength ? (aStart, aLength, 1) : (bStart, bLength, -1);
            _source.Position = longerStart + common;
            for (long i = common; i < longerLength && result == 0; i++)
            {
                if (_source.ReadByte() != 0)
                {
                    result = sign;
                }
            }
        }
        _source.Position = resume;
        return result;
    }

    /// <summary>
    /// Records why the innermost encapsulation is rejected. The message is seen only if the failure is not inside an
    /// encapsulating OCTET STRING, which never happens; it documents the reason.
    /// </summary>
    private bool FailEncapsulation(ParseFailureKind kind, long offset)
    {
        _failure = new ParseFailure(kind, offset);
        return false;
    }
}
