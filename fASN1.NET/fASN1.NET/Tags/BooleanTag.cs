using System;

namespace fASN1.NET.Tags;

public class BooleanTag : TagBase
{
    public BooleanTag(byte[]? content = null) : base(content)
    {
    }
    public BooleanTag(bool value) : this(BitConverter.GetBytes(value))
    {
    }
    public override int TagNumber => 1;
    public override string TagName => "Boolean";
    public override int TagClass => 0;
    public override bool IsConstructed => false;
}
