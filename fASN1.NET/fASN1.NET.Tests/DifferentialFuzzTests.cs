using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Seeded, deterministic fuzzing: the production parser must behave exactly like
/// <see cref="ReferenceRecursiveParser"/> on valid, odd and corrupted input. Seeds are fixed,
/// so any failure reproduces on every run and the failing input is printed in the message.
/// </summary>
public class DifferentialFuzzTests
{
    public static IEnumerable<object[]> Names => TestCorpus.Names;

    [Theory]
    [MemberData(nameof(Names))]
    public void MutatedCorpus_MatchesReference(string name)
    {
        var original = TestCorpus.Get(name);
        var rng = new Random(StableSeed(name));
        for (int i = 0; i < 1500; i++)
        {
            Differential.AssertSameAsReference(Mutate(original, rng));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void RandomBerTrees_MatchReference(int seed)
    {
        var rng = new Random(seed * 7919);
        for (int i = 0; i < 1500; i++)
        {
            var data = BerGenerator.Document(rng);
            Differential.AssertSameAsReference(data);
            if (rng.Next(2) == 0)
                Differential.AssertSameAsReference(Mutate(data, rng));
        }
    }

    [Fact]
    public void ThrowingFactory_PropagatesTheSameWay_Corpus()
    {
        var data = TestCorpus.Get("file:cms_signed_ber_indefinite");
        var factory = new Differential.RecordingFactory(new Factory.DefaultTagFactory());
        ReferenceRecursiveParser.TryDeserialize(data, factory, out _, out _);
        for (int n = 1; n <= factory.Calls.Count + 1; n++)
        {
            var throwOn = n;
            Differential.AssertSameAsReference(data, () => new Differential.ThrowingFactory(throwOn));
        }
    }

    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    public void ThrowingFactory_PropagatesTheSameWay_RandomTrees(int seed)
    {
        var rng = new Random(seed);
        for (int i = 0; i < 200; i++)
        {
            var data = BerGenerator.Document(rng);
            var throwOn = rng.Next(1, 40);
            Differential.AssertSameAsReference(data, () => new Differential.ThrowingFactory(throwOn));
        }
    }

    private static int StableSeed(string s)
    {
        unchecked
        {
            int h = 17;
            foreach (var c in s)
                h = (h * 31) + c;
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
    private static readonly byte[] PrimitiveTags = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x0A, 0x0C, 0x13, 0x16, 0x17, 0x18, 0x1E, 0x80, 0x81, 0x85, 0x41, 0xC3, 0x0D, 0x00];
    private static readonly byte[] ConstructedTags = [0x30, 0x31, 0xA0, 0xA1, 0xA3, 0xA7, 0x61, 0xE2, 0x24, 0x2C];

    public static byte[] Document(Random rng) => Node(rng, depth: 0);

    private static byte[] Node(Random rng, int depth)
    {
        int roll = rng.Next(100);
        if (depth >= 6 || roll < 40)
            return Primitive(rng, depth);
        if (roll < 55)
            return Encapsulating(rng, depth);
        return Constructed(rng, depth);
    }

    private static byte[] Primitive(Random rng, int depth)
    {
        var tag = PrimitiveTags[rng.Next(PrimitiveTags.Length)];
        var content = new byte[rng.Next(rng.Next(10) == 0 ? 300 : 6)];
        rng.NextBytes(content);
        if (tag == 0x03 && content.Length > 0 && rng.Next(2) == 0)
            content[0] = 0;
        return Tlv(tag, content, rng, allowIndefinite: false);
    }

    private static byte[] Encapsulating(Random rng, int depth)
    {
        var body = new List<byte>();
        int count = 1 + rng.Next(3);
        for (int i = 0; i < count; i++)
            body.AddRange(Node(rng, depth + 1));
        bool bitString = rng.Next(4) == 0;
        if (bitString)
            body.Insert(0, 0x00);
        return Tlv(bitString ? (byte)0x03 : (byte)0x04, body.ToArray(), rng, allowIndefinite: false);
    }

    private static byte[] Constructed(Random rng, int depth)
    {
        var tag = ConstructedTags[rng.Next(ConstructedTags.Length)];
        var body = new List<byte>();
        int count = rng.Next(5);
        for (int i = 0; i < count; i++)
        {
            body.AddRange(Node(rng, depth + 1));
            if (rng.Next(25) == 0)
                body.AddRange([0x00, 0x00]);
        }
        return Tlv(tag, body.ToArray(), rng, allowIndefinite: true);
    }

    private static byte[] Tlv(byte tag, byte[] content, Random rng, bool allowIndefinite)
    {
        var result = new List<byte> { tag };
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
                len = [(byte)(0x80 | padded.Length), .. padded];
        }
        result.AddRange(len);
        result.AddRange(content);
        return result.ToArray();
    }
}
