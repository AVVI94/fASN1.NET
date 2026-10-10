namespace fASN1.NET.Tags;

public class Null : TagBase
{
    public Null() : base()
    {
    }
    public override int TagNumber => 5;
    public override string TagName => "Null";
    public override int TagClass => 0;
    public override bool IsConstructed => false;
}
