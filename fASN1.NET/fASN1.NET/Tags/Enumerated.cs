namespace fASN1.NET.Tags;

public class Enumerated : TagBase
{
    public Enumerated(byte[]? content = null) : base(content)
    {
    }
    public override int TagNumber => 10;
    public override string TagName => "Enumerated";
    public override int TagClass => 0;
    public override bool IsConstructed => false;
}
