using fASN1.NET.Tags;
using Xunit.Sdk;

namespace fASN1.NET.Tests.Infrastructure;

/// <summary>
/// Hand-written description of a parsed node. Every property of the actual <see cref="ITag"/>
/// is compared, so a test written with this DSL pins the complete shape of the tree:
/// node types, identity, content bytes, child count and child order.
/// </summary>
internal sealed record Expected(
    Type Type,
    int TagNumber,
    string TagName,
    int TagClass,
    bool IsConstructed,
    bool IsUniversal,
    bool IsEoc,
    byte[] Content,
    Expected[] Children)
{
    /// <summary>Universal primitive leaf with fixed TagNumber.</summary>
    public static Expected Leaf<T>(int tagNumber, string name, string hex) where T : ITag
        => new(typeof(T), tagNumber, name, 0, false, true, false, Convert.FromHexString(hex), []);

    /// <summary>Universal node that holds children (SEQUENCE, SET or an encapsulating OCTET STRING).</summary>
    public static Expected Node<T>(int tagNumber, string name, params Expected[] children) where T : ITag
        => new(typeof(T), tagNumber, name, 0, true, true, false, [], children);

    public static Expected Seq(params Expected[] children) => Node<Sequence>(48, "Sequence", children);
    public static Expected Set(params Expected[] children) => Node<Tags.Set>(49, "Set", children);
    public static Expected Int(string hex) => Leaf<Integer>(2, "Integer", hex);
    public static Expected Octets(string hex) => Leaf<OctetString>(4, "OctetString", hex);
    public static Expected OctetsWith(params Expected[] children) => Node<OctetString>(4, "OctetString", children);
    public static Expected Bits(string hex) => Leaf<BitString>(3, "BitString", hex);
    public static Expected Utf8(string hex) => Leaf<Utf8String>(12, "UTF8String", hex);
    public static Expected Null() => Leaf<Tags.Null>(5, "Null", "");
    public static Expected Eoc() => new(typeof(Tags.Eoc), 0, "Eoc", 0, false, true, true, [], []);

    /// <summary>Context-specific node; <paramref name="identifier"/> is the full identifier byte (e.g. 0xA0).</summary>
    public static Expected Ctx(int identifier, string hex)
        => new(ContextType(identifier), identifier, $"[{identifier & 0x1F}]", 2, false, false, false, Convert.FromHexString(hex), []);

    public static Expected CtxWith(int identifier, params Expected[] children)
        => new(ContextType(identifier), identifier, $"[{identifier & 0x1F}]", 2, true, false, false, [], children);

    static Type ContextType(int identifier) => (identifier & 0x1F) switch
    {
        0 => typeof(ContextSpecific_0),
        1 => typeof(ContextSpecific_1),
        2 => typeof(ContextSpecific_2),
        3 => typeof(ContextSpecific_3),
        _ => typeof(ContextSpecificTag),
    };

    public void AssertMatches(ITag actual) => AssertMatches(actual, "root");

    private void AssertMatches(ITag actual, string path)
    {
        Check(path, "type", Type, actual.GetType());
        Check(path, nameof(TagNumber), TagNumber, actual.TagNumber);
        Check(path, nameof(TagName), TagName, actual.TagName);
        Check(path, nameof(TagClass), TagClass, actual.TagClass);
        Check(path, nameof(IsConstructed), IsConstructed, actual.IsConstructed);
        Check(path, nameof(IsUniversal), IsUniversal, actual.IsUniversal);
        Check(path, nameof(IsEoc), IsEoc, actual.IsEoc);
        Check(path, nameof(Content), Convert.ToHexString(Content), Convert.ToHexString(actual.Content));
        Check(path, "Children.Count", Children.Length, actual.Children.Count);
        for (int i = 0; i < Children.Length; i++)
            Children[i].AssertMatches(actual.Children[i], $"{path}/{i}");
    }

    private static void Check<T>(string path, string property, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new XunitException($"Mismatch at {path}, {property}:{Environment.NewLine}  expected: {expected}{Environment.NewLine}  actual:   {actual}");
    }
}
