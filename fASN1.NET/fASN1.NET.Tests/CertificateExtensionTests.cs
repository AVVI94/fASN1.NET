using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fASN1.NET.Tests.Infrastructure;
using SanIPAddress = fASN1.NET.Tags.San.IPAddress;
using SanDnsName = fASN1.NET.Tags.San.DnsName;

namespace fASN1.NET.Tests;

/// <summary>
/// Certificate extension helpers compared with the decoders in System.Security.Cryptography for every sample.
/// </summary>
public class CertificateExtensionTests
{
    public static IEnumerable<object[]> Names => TestCorpus.Names;

    [Theory]
    [MemberData(nameof(Names))]
    public void SubjectKeyIdentifier_MatchesDotNet(string name)
    {
        var expected = DotNetExtensions(name)?.OfType<X509SubjectKeyIdentifierExtension>().SingleOrDefault()?.SubjectKeyIdentifier;
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get(name));

        var found = tag.TryGetSubjectKeyIdentifier(out var ski);

        Assert.Equal(expected is not null, found);
        Assert.Equal(expected, ski);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void AuthorityKeyIdentifier_MatchesDotNet(string name)
    {
        var aki = DotNetExtensions(name)?.OfType<X509AuthorityKeyIdentifierExtension>().SingleOrDefault();
        var expected = aki?.KeyIdentifier is { } keyId ? Convert.ToHexString(keyId.Span) : null;
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get(name));

        var found = tag.TryGetAuthorityKeyIdentifier(out var actual);

        Assert.Equal(expected is not null, found);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void SubjectAlternativeName_DnsNamesAndIPAddressesMatchDotNet(string name)
    {
        var san = DotNetExtensions(name)?.OfType<X509SubjectAlternativeNameExtension>().SingleOrDefault();
        if (san is null)
            return;
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get(name));

        Assert.True(tag.TryGetSubjectAlternativeName(out var items));

        Assert.Equal(san.EnumerateDnsNames(), items.OfType<SanDnsName>().Select(i => i.Content));
        Assert.Equal(san.EnumerateIPAddresses().Select(ip => ip.ToString()), items.OfType<SanIPAddress>().Select(i => i.Content));
        Assert.Equal(san.EnumerateIPAddresses().Select(ip => Convert.ToHexString(ip.GetAddressBytes())),
                     items.OfType<SanIPAddress>().Select(i => Convert.ToHexString(i.Bytes)));
    }

    [Fact]
    public void KeyIdentifiers_AreHexEvenWhenTheBytesLookLikeTextOrDer()
    {
        // "KEYID-TEXT" used to come back as that text, and DER bytes as "" because they were read as children.
        Assert.True(Asn1Serializer.Deserialize(TestCorpus.Get("gen:rsa_leaf_keyid_text")).TryGetSubjectKeyIdentifier(out var text));
        Assert.Equal("4B455949442D54455854", text);
        Assert.True(Asn1Serializer.Deserialize(TestCorpus.Get("gen:rsa_leaf_keyid_der")).TryGetSubjectKeyIdentifier(out var der));
        Assert.Equal("3003020105", der);
    }

    [Fact]
    public void AuthorityKeyIdentifier_WithoutKeyIdentifier_IsNotFound()
    {
        // The extension names the issuer by name and serial number only; the issuer name used to be returned.
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get("gen:rsa_leaf_keyid_text"));

        Assert.False(tag.TryGetAuthorityKeyIdentifier(out var aki));
        Assert.Null(aki);
    }

    [Fact]
    public void IPAddress_WithPrintableBytes_IsFormattedAsAnAddress()
    {
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get("gen:rsa_leaf_keyid_text"));

        Assert.True(tag.TryGetSubjectAlternativeName(out var items));

        Assert.Equal(["65.66.67.68", "10.0.0.1"], items.OfType<SanIPAddress>().Select(i => i.Content));
    }

    [Theory]
    [InlineData("C0000211", "192.0.2.17")]
    [InlineData("20010DB8000000000000000000000017", "2001:db8::17")]
    [InlineData("C0000200FFFFFF00", "192.0.2.0/255.255.255.0")]
    [InlineData("C00002", "C00002")]
    public void IPAddress_FormatsByLength(string hex, string expected)
    {
        Assert.Equal(expected, new SanIPAddress(Convert.FromHexString(hex)).Content);
    }

    private static List<X509Extension>? DotNetExtensions(string name)
    {
        try
        {
#pragma warning disable SYSLIB0057 // the corpus holds raw DER; X509CertificateLoader is .NET 9+
            using var cert = new X509Certificate2(TestCorpus.Get(name));
#pragma warning restore SYSLIB0057
            if (!cert.RawData.AsSpan().SequenceEqual(TestCorpus.Get(name)))
                return null; // a CMS blob loads as one of the certificates inside it
            if (cert.Version < 3)
                return null; // v1 and v2 certificates are not supported by the helpers
            return cert.Extensions.Cast<X509Extension>().ToList();
        }
        catch (CryptographicException)
        {
            return null; // not a certificate
        }
    }
}
