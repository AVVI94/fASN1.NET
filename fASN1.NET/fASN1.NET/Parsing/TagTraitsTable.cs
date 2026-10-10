using System.Buffers;
using fASN1.NET.Factory;

namespace fASN1.NET.Parsing;

/// <summary>
/// <see cref="TagTraits"/> for all 256 identifier bytes of one factory.
/// </summary>
/// <remarks>
/// The default factory's table is built once and shared. For a custom factory a table is rented for
/// one parse and filled on demand: each identifier is asked of the factory once, by creating a tag.
/// The decisions depend only on a freshly created tag, as they did when the parser created every tag
/// before deciding.
/// </remarks>
internal readonly struct TagTraitsTable
{
    private const int IdentifierCount = 256;

    /// <summary>Built once and never modified afterwards, so it is safe to share between threads.</summary>
    private static readonly TagTraits[] DefaultFactoryTraits = BuildDefaultFactoryTraits();

    private readonly TagTraits[] _traits;
    private readonly ITagFactory _factory;
    private readonly bool _isRented;

    private TagTraitsTable(TagTraits[] traits, ITagFactory factory, bool isRented)
    {
        _traits = traits;
        _factory = factory;
        _isRented = isRented;
    }

    /// <summary>Gets the table for <paramref name="factory"/>. Call <see cref="Dispose"/> when the parse ends.</summary>
    public static TagTraitsTable ForFactory(ITagFactory factory)
        => ReferenceEquals(factory, DefaultTagFactory.Instance)
            ? new TagTraitsTable(DefaultFactoryTraits, factory, isRented: false)
            : new TagTraitsTable(ArrayPool<TagTraits>.Shared.Rent(IdentifierCount), factory, isRented: true);

    public TagTraits this[int identifier]
    {
        get
        {
            ref var traits = ref _traits[identifier];
            if (!traits.IsKnown)
                traits = TagTraits.Of(_factory.Create(identifier), identifier);
            return traits;
        }
    }

    public void Dispose()
    {
        if (_isRented)
            ArrayPool<TagTraits>.Shared.Return(_traits, clearArray: true);
    }

    private static TagTraits[] BuildDefaultFactoryTraits()
    {
        var traits = new TagTraits[IdentifierCount];
        for (int identifier = 0; identifier < traits.Length; identifier++)
            traits[identifier] = TagTraits.Of(DefaultTagFactory.Instance.Create(identifier), identifier);
        return traits;
    }
}
