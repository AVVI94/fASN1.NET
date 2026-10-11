namespace fASN1.NET.Tags;

/// <summary>
/// The end-of-contents marker (universal 0), which closes an indefinite-length element.
/// </summary>
public class Eoc : TagBase
{
    /// <summary>Creates the tag.</summary>
    public Eoc() : base()
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => 0;
    /// <inheritdoc/>
    public override string TagName => "Eoc";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => false;
    /// <inheritdoc/>
    public override bool IsEoc => true;
}
