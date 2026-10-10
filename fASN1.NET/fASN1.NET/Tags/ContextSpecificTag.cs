using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class ContextSpecificTag : TagBase
{
    private readonly int _tagNumber;

    public ContextSpecificTag(int tagNumber, byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
        _tagNumber = tagNumber;
    }
    public override int TagNumber => _tagNumber;
    public override string TagName => TagNames.ContextSpecific(_tagNumber);
    public override int TagClass => 2;
    public override bool IsUniversal => false;
}
