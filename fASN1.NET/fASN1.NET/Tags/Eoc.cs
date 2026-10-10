namespace fASN1.NET.Tags;

public class Eoc : TagBase
{
    public Eoc() : base()
    {
    }
    public override int TagNumber => 0;
    public override string TagName => "Eoc";
    public override int TagClass => 0;
    public override bool IsConstructed => false;
    public override bool IsEoc => true;
}
