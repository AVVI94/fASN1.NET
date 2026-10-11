using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fASN1.NET.Oid;
using fASN1.NET.Tags.San;

namespace fASN1.NET.Tests;

/// <summary>
/// The certificate helpers on structures the corpus does not cover: critical extensions, multi-valued names,
/// rarer GeneralName forms, BER extension values and look-alikes that are not certificates or requests.
/// </summary>
public class CertificateHelperTests
{
    private static readonly DateTimeOffset _notBefore = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _notAfter = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CriticalExtensions_AreFound()
    {
        // A critical flag sits between extnID and extnValue, so extnValue used to be read from the flag.
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("critical.example.test");
        var tag = Issue(r =>
        {
            r.CertificateExtensions.Add(san.Build(critical: true));
            r.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new System.Security.Cryptography.Oid(OID.EKU_SERVER_AUTH)], critical: true));
            r.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension([0x01, 0x02, 0x03], critical: true));
            r.CertificateExtensions.Add(new X509Extension(OID.SUBJECT_DIRECTORY_ATTRIBUTES, SubjectDirectoryAttributes("CZ"), critical: true));
        });

        Assert.True(tag.TryGetSubjectAlternativeName(out var names));
        Assert.Equal("critical.example.test", Assert.IsType<DnsName>(Assert.Single(names)).Content);
        Assert.True(tag.TryGetExtendedKeyUsage(out var eku));
        Assert.True(eku.ServerAuth);
        Assert.True(tag.TryGetSubjectKeyIdentifier(out var ski));
        Assert.Equal("010203", ski);
        Assert.True(tag.TryGetSubjectDirectoryAttributesFromCertificate(out var sda));
        Assert.Equal(["CZ"], sda.CountryOfCitizenship);
    }

    [Fact]
    public void ExtensionValueInBerForm_IsStillRead()
    {
        // GeneralNames with a long-form length under 128: valid BER, so the parser keeps extnValue as raw bytes.
        byte[] sanValue = [0x30, 0x81, 0x0A, 0x82, 0x08, .. "ber.test"u8];
        var tag = Issue(r => r.CertificateExtensions.Add(new X509Extension(OID.SUBJECT_ALT_NAME, sanValue, false)));

        Assert.True(tag.TryGetSubjectAlternativeName(out var names));
        Assert.Equal("ber.test", Assert.IsType<DnsName>(Assert.Single(names)).Content);
    }

    [Fact]
    public void MissingSubjectAlternativeName_IsNotFound()
    {
        var tag = Issue(_ => { });

        Assert.False(tag.TryGetSubjectAlternativeName(out var names));
        Assert.Empty(names);
        Assert.False(new Tags.Sequence().TryGetSubjectAlternativeName(out _)); // not a certificate either
    }

    [Fact]
    public void MultiValuedRdn_EveryAttributeIsFound()
    {
        // CN=Jan Novák + serialNumber=ICA-1 in one RDN; only single-attribute RDNs used to be matched.
        var subject = new X500DistinguishedName(Name(
            [("2.5.4.6", "CZ")],
            [("2.5.4.3", "Jan Novák"), ("2.5.4.5", "ICA-1")]));
        var tag = Issue(_ => { }, subject);

        Assert.True(tag.TryGetCertificateSubjectItem(SubjectItemKind.CommonName, false, out var cn));
        Assert.Equal(["Jan Novák"], cn);
        Assert.True(tag.TryGetCertificateSubjectItem(SubjectItemKind.SerialNumber, false, out var serial));
        Assert.Equal(["ICA-1"], serial);
        Assert.True(tag.TryGetCertificateSubjectItem(SubjectItemKind.CountryName, false, out var country));
        Assert.Equal(["CZ"], country);
    }

    [Fact]
    public void RareGeneralNames_AreReadWithTheirRfc5280Tagging()
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            // x400Address [3] IMPLICIT ORAddress: the [3] element is the ORAddress SEQUENCE.
            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3)))
            {
                using (writer.PushSequence())
                {
                    writer.WriteCharacterString(UniversalTagNumber.PrintableString, "CZ");
                }
            }
            // directoryName [4] EXPLICIT Name with a multi-valued RDN
            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4)))
            {
                writer.WriteEncodedValue(Name([("2.5.4.3", "a"), ("2.5.4.11", "b")]));
            }
            // ediPartyName [5] IMPLICIT SEQUENCE { nameAssigner [0] EXPLICIT DirectoryString, partyName [1] ... }
            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 5)))
            {
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    writer.WriteCharacterString(UniversalTagNumber.UTF8String, "assigner");
                }
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
                {
                    writer.WriteCharacterString(UniversalTagNumber.UTF8String, "party");
                }
            }
            // registeredID [8] IMPLICIT OBJECT IDENTIFIER
            writer.WriteObjectIdentifier("1.3.6.1.4.1.23624.1", new Asn1Tag(TagClass.ContextSpecific, 8));
        }
        var tag = Issue(r => r.CertificateExtensions.Add(new X509Extension(OID.SUBJECT_ALT_NAME, writer.Encode(), false)));

        Assert.True(tag.TryGetSubjectAlternativeName(out var names));

        Assert.Collection(names,
            x400 => Assert.Equal("CZ", Assert.Single(Assert.Single(Assert.IsType<X400Address>(x400).Content.Children).Children).ContentToString()),
            dir => Assert.Equal(["a", "b"], Assert.IsType<DirectoryName>(dir).Content.Select(r => r.Value)),
            edi =>
            {
                var party = Assert.IsType<EdiPartyName>(edi);
                Assert.Equal("assigner", party.NameAssigner);
                Assert.Equal("party", party.PartyName);
            },
            id => Assert.Equal("1.3.6.1.4.1.23624.1", Assert.IsType<RegisteredID>(id).Content.Value));
    }

    [Fact]
    public void SubjectDirectoryAttributes_EveryCountryIsRead()
    {
        var tag = Issue(r => r.CertificateExtensions.Add(new X509Extension(OID.SUBJECT_DIRECTORY_ATTRIBUTES, SubjectDirectoryAttributes("CZ", "SK"), false)));

        Assert.True(tag.TryGetSubjectDirectoryAttributesFromCertificate(out var sda));
        Assert.Equal(["CZ", "SK"], sda.CountryOfCitizenship);
    }

    [Fact]
    public void Request_IsRecognizedAndItsExtensionsRead()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=request.example.test", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
        var tag = Asn1Serializer.Deserialize(request.CreateSigningRequest());

        Assert.True(tag.IsCertificateRequest());
        Assert.False(tag.IsCertificate());
        Assert.True(tag.TryGetKeyUsage(out var keyUsage));
        Assert.True(keyUsage.DigitalSignature);
        Assert.True(keyUsage.Critical);
        Assert.True(tag.TryGetCertificateRequestSubjectItem(SubjectItemKind.CommonName, out var cn));
        Assert.Equal(["request.example.test"], cn);
    }

    [Fact]
    public void RequestWithoutAttributes_IsRecognized()
    {
        // RFC 2986 requires attributes [0] even when empty, but some encoders leave it out.
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                writer.WriteInteger(0);
                writer.WriteEncodedValue(Name([("2.5.4.3", "no-attributes")]));
                using (writer.PushSequence())
                {
                    WriteAlgorithm(writer);
                    writer.WriteBitString([0x00]);
                }
            }
            WriteAlgorithm(writer);
            writer.WriteBitString([0x00]);
        }
        var tag = Asn1Serializer.Deserialize(writer.Encode());

        Assert.True(tag.IsCertificateRequest());
        Assert.True(tag.TryGetCertificateRequestSubjectItem(SubjectItemKind.CommonName, out var cn));
        Assert.Equal(["no-attributes"], cn);
        Assert.False(tag.TryGetKeyUsage(out _));
        Assert.False(tag.TryGetSubjectAlternativeName(out _));
    }

    [Fact]
    public void Version1CertificateWithSerialZero_IsNeitherARequestNorAV3Certificate()
    {
        // tbsCertificate without the version field starts with INTEGER 0, like certificationRequestInfo did.
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                writer.WriteInteger(0);
                WriteAlgorithm(writer);
                writer.WriteEncodedValue(Name([("2.5.4.3", "issuer")]));
                using (writer.PushSequence())
                {
                    writer.WriteUtcTime(_notBefore);
                    writer.WriteUtcTime(_notAfter);
                }
                writer.WriteEncodedValue(Name([("2.5.4.3", "subject")]));
                using (writer.PushSequence())
                {
                    WriteAlgorithm(writer);
                    writer.WriteBitString([0x00]);
                }
            }
            WriteAlgorithm(writer);
            writer.WriteBitString([0x00]);
        }
        var tag = Asn1Serializer.Deserialize(writer.Encode());

        Assert.False(tag.IsCertificateRequest());
        Assert.False(tag.IsCertificate());
        Assert.False(tag.TryGetCertificateRequestSubjectItem(SubjectItemKind.CommonName, out _));
    }

    [Theory]
    [InlineData("3000")]                                   // empty SEQUENCE
    [InlineData("3009020100020100030100")]                 // three elements, nothing like a certificate
    [InlineData("3009300030000301000500")]                 // four top-level children
    public void MalformedInput_HelpersReturnFalseWithoutThrowing(string hex)
    {
        var tag = Asn1Serializer.Deserialize(Convert.FromHexString(hex));

        Assert.False(tag.IsCertificate());
        Assert.False(tag.IsCertificateRequest());
        Assert.False(tag.TryGetICACertIntercon(out _));
        Assert.False(tag.TryGetExtendedKeyUsage(out _));
        Assert.False(tag.TryGetSubjectAlternativeName(out _));
        Assert.False(tag.TryGetCertificateSerialNumber(out _));
        Assert.False(tag.TryGetCertificateNotBefore(out _));
        Assert.False(tag.TryGetCertificateExtensions(out _));
        Assert.False(tag.TryGetSubjectKeyIdentifier(out _));
        Assert.False(tag.TryGetAuthorityKeyIdentifier(out _));
        Assert.False(tag.TryGetCertificateSubjectItem(SubjectItemKind.CommonName, false, out _));
        Assert.False(tag.TryGetCertificateRequestSubjectItem(SubjectItemKind.CommonName, out _));
        Assert.Null(tag.GetSubjectDirectoryAttributesRaw());
    }

    [Fact]
    public void MalformedIcaInterconnection_ReturnsFalse()
    {
        // The value has two fields instead of three; reading the third used to throw out of the Try method.
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, "1234");
            writer.WriteInteger(2);
        }
        var tag = Issue(r => r.CertificateExtensions.Add(new X509Extension(OID.ICA_CERT_INTERCONNECTION, writer.Encode(), false)));

        Assert.False(tag.TryGetICACertIntercon(out _));
    }

    [Fact]
    public void InvalidOidString_ReturnsFalse()
    {
        var tag = Issue(_ => { });

        Assert.False(tag.TryGetCertificateSubjectItem("not an oid", false, out var items));
        Assert.Empty(items);
    }

    private static Tags.ITag Issue(Action<CertificateRequest> configure, X500DistinguishedName? subject = null)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subject ?? new X500DistinguishedName("CN=helper.example.test"), key, HashAlgorithmName.SHA256);
        configure(request);
        using var certificate = request.CreateSelfSigned(_notBefore, _notAfter);
        return Asn1Serializer.Deserialize(certificate.RawData);
    }

    /// <summary>A Name with one RDN per array; an RDN with several attributes is multi-valued.</summary>
    private static byte[] Name(params (string Oid, string Value)[][] rdns)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            foreach (var rdn in rdns)
            {
                using (writer.PushSetOf())
                {
                    foreach (var (oid, value) in rdn)
                    {
                        using (writer.PushSequence())
                        {
                            writer.WriteObjectIdentifier(oid);
                            writer.WriteCharacterString(UniversalTagNumber.UTF8String, value);
                        }
                    }
                }
            }
        }
        return writer.Encode();
    }

    /// <summary>SubjectDirectoryAttributes with one countryOfCitizenship attribute holding every country.</summary>
    private static byte[] SubjectDirectoryAttributes(params string[] countries)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(OID.COUNTRY_OF_CITIZENSHIP);
                using (writer.PushSetOf())
                {
                    foreach (var country in countries)
                    {
                        writer.WriteCharacterString(UniversalTagNumber.PrintableString, country);
                    }
                }
            }
        }
        return writer.Encode();
    }

    private static void WriteAlgorithm(AsnWriter writer)
    {
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier("1.2.840.10045.4.3.2"); // ecdsa-with-SHA256
        }
    }
}
