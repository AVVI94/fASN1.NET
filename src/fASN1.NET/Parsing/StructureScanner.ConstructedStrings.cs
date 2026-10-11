using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// Strings in the constructed encoding (X.690 §8.6.4, §8.7.3, §8.23.6): a BIT STRING, OCTET STRING, restricted
/// character string or time whose contents are fragments. The string becomes one leaf node whose value is the contents
/// of its primitive fragments joined, recorded with the primitive identifier, so the factory creates the same tag as
/// for the primitive encoding. Fragments get no nodes, only <see cref="ContentsFragment"/>s.
/// </summary>
internal ref partial struct StructureScanner<TSource>
{
    /// <summary>Starts a string in the constructed encoding.</summary>
    private ScanResult EnterConstructedString(in IdentifierAndLength encoding, int fragmentTagNumber)
    {
        var nodeIndex = _nodes.Add(new ScannedNode
        {
            IdentifierOctet = encoding.IdentifierOctet & ~Ber.CONSTRUCTED_FLAG,
            TagNumber = encoding.TagNumber,
            ContentsStart = encoding.ContentsStart,
            IsConstructedString = true,
            FirstFragment = _fragments.Count,
        });
        return EnterContents(new EnclosingEncoding
        {
            Kind = EnclosingEncodingKind.ConstructedString,
            IdentifierAndLength = encoding,
            NodeIndex = nodeIndex,
            FragmentTagNumber = fragmentTagNumber,
        });
    }

    /// <summary>
    /// Reads one fragment of the innermost constructed string or constructed fragment: a primitive fragment adds its
    /// contents octets to the string, a constructed one is entered and its fragments are added in turn.
    /// </summary>
    private ScanResult ScanFragment(in IdentifierAndLength encoding)
    {
        ref var enclosing = ref _enclosing.Last;
        var fragmentTagNumber = enclosing.FragmentTagNumber;
        if ((encoding.IdentifierOctet & ~Ber.CONSTRUCTED_FLAG) != fragmentTagNumber)
        {
            var expected = fragmentTagNumber == (int)Tag.BitString ? "a BIT STRING" : "an OCTET STRING";
            return Fail(new ParseFailure(ParseFailureKind.InvalidFragment, encoding.IdentifierStart, tagName: expected));
        }
        if (Ber.IsConstructed(encoding.IdentifierOctet))
        {
            return EnterContents(new EnclosingEncoding
            {
                Kind = EnclosingEncodingKind.ConstructedFragment,
                IdentifierAndLength = encoding,
                NodeIndex = enclosing.NodeIndex,
                FragmentTagNumber = fragmentTagNumber,
            });
        }

        var stringNodeIndex = enclosing.NodeIndex;
        var skipped = SkipContents(encoding);
        if (skipped != ScanResult.Succeeded)
            return skipped;

        var fragment = new ContentsFragment { Start = encoding.ContentsStart, Length = encoding.ContentsLength!.Value };
        if (fragmentTagNumber == (int)Tag.BitString && !TryTakeUnusedBits(ref fragment, encoding.IdentifierStart, ref _nodes[stringNodeIndex]))
            return ScanResult.Failed;
        _fragments.Add(fragment);
        return ScanResult.Succeeded;
    }

    /// <summary>
    /// The contents of each BIT STRING fragment start with its own unused-bits octet, and only the last fragment may
    /// have unused bits (§8.6.4). Moves the fragment past that octet and records the count on the string's node.
    /// </summary>
    private bool TryTakeUnusedBits(ref ContentsFragment fragment, long fragmentStart, ref ScannedNode bitString)
    {
        // Unused bits in the previous fragment mean that one had to be the last.
        if (bitString.UnusedBits != 0 || fragment.Length == 0)
            return FailBitStringFragment(fragmentStart);

        var resume = _source.Position;
        _source.Position = fragment.Start;
        bitString.UnusedBits = _source.ReadByte();
        _source.Position = resume;
        if (bitString.UnusedBits > 7 || (fragment.Length == 1 && bitString.UnusedBits != 0))
            return FailBitStringFragment(fragmentStart);

        fragment.Start++;
        fragment.Length--;
        return true;
    }

    /// <summary>Leaves a constructed string: its contents length is the total of its fragments.</summary>
    private ScanResult JoinFragments(in EnclosingEncoding constructedString)
    {
        ref var node = ref _nodes[constructedString.NodeIndex];
        node.FragmentCount = _fragments.Count - node.FirstFragment;
        long length = constructedString.FragmentTagNumber == (int)Tag.BitString ? 1 : 0; // the unused-bits octet
        foreach (var fragment in _fragments.AsSpan().Slice(node.FirstFragment))
        {
            length += fragment.Length;
        }
        if (length > int.MaxValue)
            return Fail(new ParseFailure(ParseFailureKind.LengthTooLarge, constructedString.IdentifierAndLength.IdentifierStart));

        node.ContentsLength = length;
        return ScanResult.Succeeded;
    }

    private bool FailBitStringFragment(long fragmentStart)
    {
        _failure = new ParseFailure(ParseFailureKind.InvalidBitStringFragment, fragmentStart);
        return false;
    }
}
