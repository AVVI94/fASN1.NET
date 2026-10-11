namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 ENUMERATED type (universal 10).
/// </summary>
public class Enumerated : TagBase
{
    /// <summary>Creates the tag with the given content.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    public Enumerated(byte[]? content = null) : base(content)
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => 10;
    /// <inheritdoc/>
    public override string TagName => "Enumerated";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => false;
}
