namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 NULL type (universal 5).
/// </summary>
public class Null : TagBase
{
    /// <summary>Creates the tag.</summary>
    public Null() : base()
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => 5;
    /// <inheritdoc/>
    public override string TagName => "Null";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => false;
}
