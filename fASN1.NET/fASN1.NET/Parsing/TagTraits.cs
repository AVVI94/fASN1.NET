using System.Buffers;
using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>What the scan needs to know about the tag a factory creates for an identifier.</summary>
internal readonly struct TagTraits(bool constructed, bool probe, string name)
{
    /// <summary>The content is read as nested elements.</summary>
    public bool Constructed { get; } = constructed;

    /// <summary>A primitive universal OCTET STRING: its content is tried as nested elements.</summary>
    public bool Probe { get; } = probe;

    /// <summary>Tag name, used in error messages.</summary>
    public string Name { get; } = name;

    public bool IsKnown => Name is not null;

    public static TagTraits Of(ITag freshTag, int identifier)
    {
        var constructed = freshTag.IsConstructed || (identifier & 0x20) != 0;
        // A BIT STRING is always a leaf: signatures and keys would otherwise be expanded by accident.
        var probe = !constructed && freshTag.IsUniversal && freshTag.TagNumber == (int)Tag.OctetString;
        return new TagTraits(constructed, probe, freshTag.TagName);
    }
}

/// <summary>
/// Traits per identifier for the factory of one parse. For a custom factory the table is rented for the
/// call and each identifier is asked of the factory once, by creating a tag; the decisions depend only on
/// a freshly created tag, as they always have.
/// </summary>
internal readonly struct TraitsLookup
{
    /// <summary>
    /// The default factory's traits for all 256 identifiers. Built once and never modified afterwards,
    /// so it is safe to share.
    /// </summary>
    private static readonly TagTraits[] DefaultTraits = BuildDefaultTraits();

    private readonly TagTraits[] _table;
    private readonly ITagFactory _factory;
    private readonly bool _rented;

    private TraitsLookup(TagTraits[] table, ITagFactory factory, bool rented)
    {
        _table = table;
        _factory = factory;
        _rented = rented;
    }

    public static TraitsLookup For(ITagFactory factory)
        => ReferenceEquals(factory, DefaultTagFactory.Instance)
            ? new TraitsLookup(DefaultTraits, factory, rented: false)
            : new TraitsLookup(ArrayPool<TagTraits>.Shared.Rent(256), factory, rented: true);

    public TagTraits this[int identifier]
    {
        get
        {
            ref var traits = ref _table[identifier];
            if (!traits.IsKnown)
                traits = TagTraits.Of(_factory.Create(identifier), identifier);
            return traits;
        }
    }

    public void Release()
    {
        if (_rented)
            ArrayPool<TagTraits>.Shared.Return(_table, clearArray: true);
    }

    private static TagTraits[] BuildDefaultTraits()
    {
        var traits = new TagTraits[256];
        for (int i = 0; i < traits.Length; i++)
            traits[i] = TagTraits.Of(DefaultTagFactory.Instance.Create(i), i);
        return traits;
    }
}
