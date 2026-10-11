using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Seeded, deterministic fuzzing. Every input must satisfy the public contract (see <see cref="Contract"/>),
/// and whenever the System.Formats.Asn1 oracle accepts an input, the parser must accept it too and
/// produce the same tree. Seeds are fixed, so any failure reproduces on every run and the failing input
/// is printed in the message.
/// </summary>
public class FuzzTests
{
    public static IEnumerable<object[]> Names => TestCorpus.Names;

    [Theory]
    [MemberData(nameof(Names))]
    public void MutatedCorpus_SatisfiesContractAndOracle(string name)
    {
        var original = TestCorpus.Get(name);
        var rng = new Random(StableSeed(name));
        for (int i = 0; i < 1500; i++)
        {
            CheckAgainstOracle(Mutate(original, rng));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void RandomBerTrees_SatisfyContractAndOracle(int seed)
    {
        var rng = new Random(seed * 7919);
        int oracleAccepted = 0;
        const int COUNT = 1500;
        for (int i = 0; i < COUNT; i++)
        {
            var data = BerGenerator.Document(rng);
            if (CheckAgainstOracle(data))
            {
                oracleAccepted++;
            }

            if (rng.Next(2) == 0)
            {
                CheckAgainstOracle(Mutate(data, rng));
            }
        }
        // Keeps the oracle comparison from silently becoming vacuous if the generator drifts.
        Assert.True(oracleAccepted > COUNT / 2, $"only {oracleAccepted} of {COUNT} generated documents were valid BER");
    }

    /// <summary>
    /// Checks the contract and, when the oracle can decode the input, the complete tree.
    /// Returns whether the oracle accepted the input.
    /// </summary>
    private static bool CheckAgainstOracle(byte[] data)
    {
        var (tag, error) = Contract.Check(data);
        OracleNode oracle;
        try
        {
            oracle = OracleNode.Decode(data);
        }
        catch (System.Formats.Asn1.AsnContentException)
        {
            return false;
        }
        try
        {
            Assert.True(tag is not null, $"the oracle accepts the input but the parser failed: {error}");
            oracle.AssertMatches(tag!);
        }
        catch (Xunit.Sdk.XunitException ex)
        {
            throw new Xunit.Sdk.XunitException($"Input {Convert.ToHexString(data)}{Environment.NewLine}{ex.Message}");
        }
        return true;
    }

    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    public void ThrowingFactory_SatisfiesContract(int seed)
    {
        var rng = new Random(seed);
        for (int i = 0; i < 300; i++)
        {
            var data = BerGenerator.Document(rng);
            var throwOn = rng.Next(1, 40);
            Contract.Check(data, () => new Contract.ThrowingFactory(throwOn));
        }
    }

    private static int StableSeed(string s)
    {
        unchecked
        {
            int h = 17;
            foreach (var c in s)
            {
                h = (h * 31) + c;
            }

            return h;
        }
    }

    internal static byte[] Mutate(byte[] input, Random rng)
    {
        var data = input.ToList();
        int edits = 1 + rng.Next(3);
        for (int e = 0; e < edits && data.Count > 0; e++)
        {
            int pos = rng.Next(data.Count);
            switch (rng.Next(9))
            {
                case 0: data[pos] ^= (byte)(1 << rng.Next(8)); break;                  // bit flip
                case 1: data[pos] = (byte)rng.Next(256); break;                         // random byte
                case 2: data.RemoveAt(pos); break;                                      // delete
                case 3: data.Insert(pos, (byte)rng.Next(256)); break;                   // insert
                case 4: data.RemoveRange(pos, data.Count - pos); break;                 // truncate
                case 5: data[pos] = 0x80; break;                                        // make a length indefinite
                case 6: data.InsertRange(pos, [0x00, 0x00]); break;                     // stray EOC
                case 7: data[pos] = (byte)(data[pos] ^ 0x20); break;                    // toggle constructed bit
                case 8: data[pos] = (byte)(0x81 + rng.Next(6)); break;                  // long-form length marker
            }
        }
        return data.Count == 0 ? [0x30] : data.ToArray();
    }
}

/// <summary>
/// Generates random BER documents: definite and indefinite lengths, short, long and non-minimal
/// length forms, all tag classes, OCTET STRINGs that wrap encoded subtrees, BIT STRINGs,
/// stray EOCs and constructed universal string forms.
/// </summary>
internal static class BerGenerator
{
    private static readonly byte[] _primitiveTags = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x0A, 0x0C, 0x13, 0x16, 0x17, 0x18, 0x1E, 0x80, 0x81, 0x85, 0x41, 0xC3, 0x0D, 0x00];
    private static readonly byte[] _constructedTags = [0x30, 0x31, 0xA0, 0xA1, 0xA3, 0xA7, 0x61, 0xE2, 0x22];

    public static byte[] Document(Random rng) => Node(rng, depth: 0);

    private static byte[] Node(Random rng, int depth)
    {
        int roll = rng.Next(100);
        if (depth >= 6 || roll < 40)
        {
            return Primitive(rng, depth);
        }

        if (roll < 55)
        {
            return Encapsulating(rng, depth);
        }

        if (roll < 65)
        {
            return ConstructedString(rng);
        }

        return Constructed(rng, depth);
    }

    /// <summary>A BER constructed string: BIT STRING, OCTET STRING, UTF8String or IA5String split into fragments.</summary>
    private static byte[] ConstructedString(Random rng)
    {
        var (tag, fragmentTag) = rng.Next(4) switch
        {
            0 => ((byte)0x23, (byte)0x03),
            1 => ((byte)0x24, (byte)0x04),
            2 => ((byte)0x2C, (byte)0x04),
            _ => ((byte)0x36, (byte)0x04),
        };
        return Tlv([tag], Fragments(rng, fragmentTag, levels: 2), rng, allowIndefinite: true);
    }

    /// <summary>Fragments, some of them constructed in turn, and now and then an invalid one.</summary>
    private static byte[] Fragments(Random rng, byte fragmentTag, int levels)
    {
        var body = new List<byte>();
        int count = rng.Next(4);
        for (int i = 0; i < count; i++)
        {
            if (levels > 0 && rng.Next(5) == 0)
            {
                body.AddRange(Tlv([(byte)(fragmentTag | 0x20)], Fragments(rng, fragmentTag, levels - 1), rng, allowIndefinite: true));
                continue;
            }
            var data = new byte[rng.Next(6)];
            rng.NextBytes(data);
            if (fragmentTag == 0x03)
            {
                data = [(byte)(rng.Next(10) == 0 ? rng.Next(8) : 0), .. data]; // unused bits
            }

            var identifier = rng.Next(20) == 0 ? (byte)0x02 : fragmentTag;
            body.AddRange(Tlv([identifier], data, rng, allowIndefinite: false));
        }
        return body.ToArray();
    }

    private static byte[] Primitive(Random rng, int depth)
    {
        var tag = _primitiveTags[rng.Next(_primitiveTags.Length)];
        var content = new byte[rng.Next(rng.Next(10) == 0 ? 300 : 6)];
        rng.NextBytes(content);
        if (tag == 0x03 && content.Length > 0 && rng.Next(2) == 0)
        {
            content[0] = 0;
        }

        return Tlv(Identifier(tag, rng), content, rng, allowIndefinite: false);
    }

    private static byte[] Encapsulating(Random rng, int depth)
    {
        var body = new List<byte>();
        int count = 1 + rng.Next(3);
        for (int i = 0; i < count; i++)
        {
            body.AddRange(Node(rng, depth + 1));
        }

        bool bitString = rng.Next(4) == 0;
        if (bitString)
        {
            body.Insert(0, 0x00);
        }

        return Tlv([bitString ? (byte)0x03 : (byte)0x04], body.ToArray(), rng, allowIndefinite: false);
    }

    private static byte[] Constructed(Random rng, int depth)
    {
        var tag = _constructedTags[rng.Next(_constructedTags.Length)];
        var body = new List<byte>();
        int count = rng.Next(5);
        for (int i = 0; i < count; i++)
        {
            body.AddRange(Node(rng, depth + 1));
            if (rng.Next(25) == 0)
            {
                body.AddRange([0x00, 0x00]);
            }
        }
        return Tlv(Identifier(tag, rng), body.ToArray(), rng, allowIndefinite: true);
    }

    /// <summary>
    /// The identifier for <paramref name="tag"/>, or now and then the same class and constructed bit with a tag number
    /// of 31 or more in the multi-byte form.
    /// </summary>
    private static byte[] Identifier(byte tag, Random rng)
    {
        if (tag == 0x00 || rng.Next(8) != 0)
        {
            return [tag];
        }

        var number = 31 + rng.Next(5000);
        var digits = new List<byte>();
        for (var n = number; n > 0; n >>= 7)
        {
            digits.Insert(0, (byte)((n & 0x7F) | (digits.Count == 0 ? 0 : 0x80)));
        }

        return [(byte)((tag & 0xE0) | 0x1F), .. digits];
    }

    private static byte[] Tlv(byte[] identifier, byte[] content, Random rng, bool allowIndefinite)
    {
        var result = new List<byte>(identifier);
        if (allowIndefinite && rng.Next(3) == 0)
        {
            result.Add(0x80);
            result.AddRange(content);
            result.AddRange([0x00, 0x00]);
            return result.ToArray();
        }
        var len = ParsedStructureTests.EncodeLength(content.Length);
        if (rng.Next(6) == 0)
        {
            // non-minimal long form
            int extra = 1 + rng.Next(3);
            var minimal = len.Length == 1 ? [(byte)content.Length] : len.Skip(1).ToArray();
            var padded = new byte[extra + minimal.Length];
            minimal.CopyTo(padded, extra);
            if (padded.Length <= 6)
            {
                len = [(byte)(0x80 | padded.Length), .. padded];
            }
        }
        result.AddRange(len);
        result.AddRange(content);
        return result.ToArray();
    }
}
