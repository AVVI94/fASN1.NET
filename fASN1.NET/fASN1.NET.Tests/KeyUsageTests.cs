using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Key usage decoding checked against .NET's own <see cref="X509KeyUsageExtension"/> for every
/// certificate and certificate request in the corpus, plus hand-written BIT STRINGs.
/// </summary>
public class KeyUsageTests
{
    public static IEnumerable<object[]> Names => TestCorpus.Names;

    [Theory]
    [MemberData(nameof(Names))]
    public void TryGetKeyUsage_MatchesDotNet(string name)
    {
        var data = TestCorpus.Get(name);
        var expected = DotNetKeyUsage(data);
        var tag = Asn1Serializer.Deserialize(data);

        var found = tag.TryGetKeyUsage(out var ku);

        Assert.Equal(expected is not null, found);
        if (expected is null)
            return;
        Assert.Equal(expected.Value.Flags, ToFlags(ku));
        Assert.Equal(expected.Value.Critical, ku.Critical);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TagToString_ShowsTheSameKeyUsage(string name)
    {
        var data = TestCorpus.Get(name);
        var tag = Asn1Serializer.Deserialize(data);
        if (!tag.TryGetKeyUsage(out var ku))
            return;

        AssertRendered(tag, ku);
    }

    [Theory]
    [InlineData("0780", X509KeyUsageFlags.DigitalSignature)]
    [InlineData("06C0", X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation)]
    [InlineData("0520", X509KeyUsageFlags.KeyEncipherment)]
    [InlineData("05A0", X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment)]
    [InlineData("0106", X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign)]
    [InlineData("0001", X509KeyUsageFlags.EncipherOnly)]
    [InlineData("070080", X509KeyUsageFlags.DecipherOnly)]
    [InlineData("07FF80", (X509KeyUsageFlags)0x80FF)]
    [InlineData("00", X509KeyUsageFlags.None)]
    public void BitString_IsDecodedPerRfc5280(string bitStringContent, X509KeyUsageFlags expected)
    {
        // SEQUENCE { OID keyUsage, OCTET STRING { BIT STRING } }, as it appears in an extension list.
        var bits = Convert.FromHexString(bitStringContent);
        var extension = new Tags.Sequence([new Tags.ObjectIdentifier("2.5.29.15"), new Tags.OctetString(children: [new Tags.UniversalTag(3, bits)])]);
        var tag = Asn1Serializer.Deserialize(Asn1Serializer.Serialize(new Tags.Sequence([extension])));

        Assert.True(tag.TryGetKeyUsage(out var ku));
        Assert.Equal(expected, ToFlags(ku));
        AssertRendered(tag, ku);
    }

    [Fact]
    public void Constructors_DoNotSetDecipherOnlyImplicitly()
    {
        Assert.False(new KeyUsage(true).DecipherOnly);
        Assert.False(new KeyUsage(true, digitalSignature: false).DecipherOnly);
        Assert.Equal("", new KeyUsage(false).ToString());
        Assert.True(new KeyUsage(false, false, false, false, false, false, false, false, false, decipherOnly: true).DecipherOnly);
    }

    /// <summary>TagToString shows the key usage BIT STRING as the same flag names (no wrapping, to compare whole lines).</summary>
    private static void AssertRendered(Tags.ITag tag, KeyUsage ku)
    {
        var lines = Asn1Serializer.TagToString(tag, o => o.MaximumContentLineLength = 0).Split(Environment.NewLine);
        var flags = ku.ToString();
        Assert.Contains(lines, line => line.TrimStart(' ', '|') == (flags.Length == 0 ? "BitString" : "BitString " + flags));
    }

    private static X509KeyUsageFlags ToFlags(KeyUsage ku)
    {
        X509KeyUsageFlags flags = 0;
        if (ku.DigitalSignature) flags |= X509KeyUsageFlags.DigitalSignature;
        if (ku.NonRepudiation) flags |= X509KeyUsageFlags.NonRepudiation;
        if (ku.KeyEncipherment) flags |= X509KeyUsageFlags.KeyEncipherment;
        if (ku.DataEncipherment) flags |= X509KeyUsageFlags.DataEncipherment;
        if (ku.KeyAgreement) flags |= X509KeyUsageFlags.KeyAgreement;
        if (ku.KeyCertSign) flags |= X509KeyUsageFlags.KeyCertSign;
        if (ku.CRLSign) flags |= X509KeyUsageFlags.CrlSign;
        if (ku.EncipherOnly) flags |= X509KeyUsageFlags.EncipherOnly;
        if (ku.DecipherOnly) flags |= X509KeyUsageFlags.DecipherOnly;
        return flags;
    }

    /// <summary>The key usage .NET finds in a certificate or certificate request, or null if there is none.</summary>
    private static (X509KeyUsageFlags Flags, bool Critical)? DotNetKeyUsage(byte[] data)
    {
        IEnumerable<X509Extension> extensions;
        try
        {
#pragma warning disable SYSLIB0057 // the corpus holds raw DER; X509CertificateLoader is .NET 9+
            extensions = new X509Certificate2(data).Extensions.Cast<X509Extension>();
#pragma warning restore SYSLIB0057
        }
        catch (CryptographicException)
        {
            try
            {
                extensions = CertificateRequest.LoadSigningRequest(data, HashAlgorithmName.SHA256,
                    CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions | CertificateRequestLoadOptions.SkipSignatureValidation).CertificateExtensions;
            }
            catch (CryptographicException)
            {
                return null; // not a certificate or request (e.g. CMS or synthetic data)
            }
        }

        var ext = extensions.FirstOrDefault(e => e.Oid?.Value == "2.5.29.15");
        if (ext is null)
            return null;
        return (new X509KeyUsageExtension(ext, ext.Critical).KeyUsages, ext.Critical);
    }
}
