using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class UniversalTag : TagBase
{
    private readonly int _tagNumber;

    public UniversalTag(int tagNumber, byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
        _tagNumber = tagNumber;
    }
    public override int TagNumber => _tagNumber;
    public override string TagName => "Universal_" + TagNumber;
    public override int TagClass => 0;
    public override bool IsUniversal => true;
}
