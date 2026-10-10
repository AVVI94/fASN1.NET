using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class Application : TagBase
{
    private readonly int _tagNumber;

    public Application(int tagNumber, byte[]? content = null, List<ITag>? children = null) : base(content, children)
    {
        _tagNumber = tagNumber;
    }
    public override int TagNumber => _tagNumber;
    public override string TagName => TagNames.Application(_tagNumber);
    public override int TagClass => _tagNumber >> 6;
    public override bool IsConstructed => (_tagNumber & 0x20) != 0;
    public override bool IsEoc => TagClass == 0x00 && TagNumber == 0x00;
}
