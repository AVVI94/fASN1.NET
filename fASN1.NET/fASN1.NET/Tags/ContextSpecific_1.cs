using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class ContextSpecific_1 : TagBase
{
    private readonly int _tagNumber;

    public ContextSpecific_1(int tagNumber, byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
        _tagNumber = tagNumber;
    }
    public override int TagNumber => _tagNumber;
    public override string TagName => "[1]";
    public override int TagClass => 2;
    public override bool IsUniversal => false;
}
