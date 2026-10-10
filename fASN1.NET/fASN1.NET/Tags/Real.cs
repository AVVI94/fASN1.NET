namespace fASN1.NET.Tags;

public class Real : TagBase
{
    public Real(byte[]? content = null) : base(content)
    {
    }
    public override int TagNumber => 9;
    public override string TagName => "Real";
    public override int TagClass => 0;
    public override bool IsConstructed => false;
}
