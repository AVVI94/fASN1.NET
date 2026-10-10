using System.Buffers;
using fASN1.NET.Factory;
using fASN1.NET.Tags;

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
    private const int IDENTIFIER_COUNT = 256;

    /// <summary>Built once and never modified afterwards, so it is safe to share between threads.</summary>
    private static readonly TagTraits[] _defaultFactoryTraits = BuildDefaultFactoryTraits();

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
            ? new TagTraitsTable(_defaultFactoryTraits, factory, isRented: false)
            : new TagTraitsTable(ArrayPool<TagTraits>.Shared.Rent(IDENTIFIER_COUNT), factory, isRented: true);

    public TagTraits this[int identifier]
    {
        get
        {
            ref var traits = ref _traits[identifier];
            if (!traits.IsKnown)
                traits = TagTraits.Of(Create(_factory, identifier, identifier & Ber.TAG_NUMBER_MASK), identifier);
            return traits;
        }
    }

    /// <summary>
    /// Traits of an element: from the table when the tag number fits the first identifier byte, otherwise from a tag
    /// created for it, as the table is indexed by that byte alone. Multi-byte tag numbers are rare, so the allocation
    /// does not matter.
    /// </summary>
    public TagTraits ForElement(int identifier, int tagNumber)
        => tagNumber < Ber.TAG_NUMBER_MASK ? this[identifier] : TagTraits.Of(Create(_factory, identifier, tagNumber), identifier);

    /// <summary>Asks <paramref name="factory"/> for the tag of a first identifier byte and tag number.</summary>
    public static ITag Create(ITagFactory factory, int identifier, int tagNumber)
        => factory.Create(identifier >> 6, (identifier & Ber.CONSTRUCTED_FLAG) != 0, tagNumber);

    public void Dispose()
    {
        if (_isRented)
            ArrayPool<TagTraits>.Shared.Return(_traits, clearArray: true);
    }

    private static TagTraits[] BuildDefaultFactoryTraits()
    {
        var traits = new TagTraits[IDENTIFIER_COUNT];
        for (int identifier = 0; identifier < traits.Length; identifier++)
            traits[identifier] = TagTraits.Of(Create(DefaultTagFactory.Instance, identifier, identifier & Ber.TAG_NUMBER_MASK), identifier);
        return traits;
    }
}
