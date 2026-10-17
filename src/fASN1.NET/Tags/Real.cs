namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 REAL type (universal 9).
/// </summary>
public class Real : TagBase
{
    /// <summary>Creates the tag with the given content.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    public Real(byte[]? content = null) : base(content)
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => 9;
    /// <inheritdoc/>
    public override string TagName => "Real";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => false;
}
