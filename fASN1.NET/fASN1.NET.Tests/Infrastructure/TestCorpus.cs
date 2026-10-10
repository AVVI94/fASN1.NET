using System.Formats.Asn1;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace fASN1.NET.Tests.Infrastructure;

/// <summary>
/// Inputs shared by the structure, oracle and differential tests.
/// <list type="bullet">
/// <item>Real samples copied from the existing tests, console tester and benchmark (TestData/corpus).</item>
/// <item>Certificates and requests generated from fixed keys, so their bytes are the same on every run.</item>
/// <item>Synthetic documents written with <see cref="AsnWriter"/> in CER mode, which uses indefinite lengths.</item>
/// </list>
/// </summary>
internal static class TestCorpus
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "TestData");
    private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> _all = new(Build);

    public static IReadOnlyDictionary<string, byte[]> All => _all.Value;

    /// <summary>Names of every sample, for <c>[MemberData]</c>.</summary>
    public static IEnumerable<object[]> Names => All.Keys.OrderBy(k => k, StringComparer.Ordinal).Select(k => new object[] { k });

    /// <summary>Samples that are DER, so Serialize must reproduce them byte for byte.</summary>
    public static IEnumerable<object[]> DerNames => All.Keys.Where(IsDer).OrderBy(k => k, StringComparer.Ordinal).Select(k => new object[] { k });

    public static bool IsDer(string name) => !name.Contains("ber") && !name.Contains("cer");

    public static byte[] Get(string name) => All[name];

    private static IReadOnlyDictionary<string, byte[]> Build()
    {
        var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "corpus"), "*.b64"))
            result["file:" + Path.GetFileNameWithoutExtension(file)] = Convert.FromBase64String(File.ReadAllText(file).Trim());

        foreach (var (name, data) in Generated())
            result["gen:" + name] = data;

        foreach (var (name, data) in Synthetic())
            result["syn:" + name] = data;
        return result;
    }

    private static RSA LoadRsa(string file)
    {
        var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(File.ReadAllText(Path.Combine(Root, "keys", file))), out _);
        return rsa;
    }

    private static ECDsa LoadEc()
    {
        var ec = ECDsa.Create();
        ec.ImportPkcs8PrivateKey(Convert.FromBase64String(File.ReadAllText(Path.Combine(Root, "keys", "ecp256.pkcs8.b64"))), out _);
        return ec;
    }

    private static readonly DateTimeOffset NotBefore = new(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset NotAfter = new(2051, 6, 7, 8, 9, 10, TimeSpan.Zero); // GeneralizedTime range

    private static IEnumerable<(string, byte[])> Generated()
    {
        using var caKey = LoadRsa("rsa2048_ca.pkcs8.b64");
        using var leafKey = LoadRsa("rsa2048_leaf.pkcs8.b64");
        using var ecKey = LoadEc();

        var caName = new X500DistinguishedNameBuilder();
        caName.AddCountryOrRegion("CZ");
        caName.AddOrganizationName("fASN1 Test Authority");
        caName.AddCommonName("fASN1 Test Root CA");
        var caReq = new CertificateRequest(caName.Build(), caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 1, true));
        caReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        caReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(caReq.PublicKey, false));
        using var ca = caReq.Create(caReq.SubjectName, X509SignatureGenerator.CreateForRSA(caKey, RSASignaturePadding.Pkcs1), NotBefore, NotAfter, [0x01, 0x02]);
        yield return ("rsa_root_ca", ca.RawData);

        var leafName = new X500DistinguishedNameBuilder();
        leafName.AddCountryOrRegion("CZ");
        leafName.AddStateOrProvinceName("Praha");
        leafName.AddLocalityName("Praha 1");
        leafName.AddOrganizationName("Příliš žluťoučký kůň s.r.o.");
        leafName.AddOrganizationalUnitName("Engineering");
        leafName.AddEmailAddress("pki@example.test");
        leafName.Add("2.5.4.5", "ICA - 10011663", UniversalTagNumber.PrintableString);
        leafName.Add("2.5.4.97", "NTRCZ-12345678", UniversalTagNumber.UTF8String);
        leafName.Add("2.5.4.12", "Ředitel", UniversalTagNumber.BMPString);
        leafName.AddCommonName("leaf.example.test");

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("leaf.example.test");
        san.AddDnsName("www.leaf.example.test");
        san.AddEmailAddress("leaf@example.test");
        san.AddIpAddress(IPAddress.Parse("192.0.2.17"));
        san.AddIpAddress(IPAddress.Parse("2001:db8::17"));
        san.AddUri(new Uri("https://leaf.example.test/path?q=1"));
        san.AddUserPrincipalName("user@example.test");

        var flagSets = new (string Name, X509KeyUsageFlags Flags)[]
        {
            ("ku_digsig", X509KeyUsageFlags.DigitalSignature),
            ("ku_keyenc", X509KeyUsageFlags.KeyEncipherment),
            ("ku_digsig_nonrep", X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation),
            ("ku_all", X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.KeyEncipherment
                       | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyAgreement | X509KeyUsageFlags.KeyCertSign
                       | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.EncipherOnly | X509KeyUsageFlags.DecipherOnly),
        };

        foreach (var (name, flags) in flagSets)
        {
            var req = new CertificateRequest(leafName.Build(), leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            req.CertificateExtensions.Add(new X509KeyUsageExtension(flags, name != "ku_digsig"));
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.1"), new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.2"), new System.Security.Cryptography.Oid("1.3.6.1.4.1.23624.99.1")], false));
            req.CertificateExtensions.Add(san.Build());
            req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
            req.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(ca, true, true));
            req.CertificateExtensions.Add(new X509Extension("2.5.29.32", CertificatePolicies(), false));
            req.CertificateExtensions.Add(new X509Extension("1.3.6.1.4.1.23624.99.2", OpaqueExtensionValue(), false));
            using var leaf = req.Create(ca.SubjectName, X509SignatureGenerator.CreateForRSA(caKey, RSASignaturePadding.Pkcs1), NotBefore, NotAfter, [0x00, 0x9A, 0x13, 0xFA]);
            yield return ("rsa_leaf_" + name, leaf.RawData);

            if (name == "ku_all")
                yield return ("rsa_csr_" + name, req.CreateSigningRequest());
        }

        var ecReq = new CertificateRequest("CN=ec.example.test, O=fASN1", ecKey, HashAlgorithmName.SHA384);
        ecReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        ecReq.CertificateExtensions.Add(san.Build());
        yield return ("ec_csr", ecReq.CreateSigningRequest());
    }

    /// <summary>CertificatePolicies with a CPS URI and a user notice, mixing several string types.</summary>
    private static byte[] CertificatePolicies()
    {
        var w = new AsnWriter(AsnEncodingRules.DER);
        using (w.PushSequence())
        {
            using (w.PushSequence())
            {
                w.WriteObjectIdentifier("1.3.6.1.4.1.23624.10.1.30.1.1");
                using (w.PushSequence())
                {
                    using (w.PushSequence())
                    {
                        w.WriteObjectIdentifier("1.3.6.1.5.5.7.2.1");
                        w.WriteCharacterString(UniversalTagNumber.IA5String, "http://www.example.test/cps");
                    }
                    using (w.PushSequence())
                    {
                        w.WriteObjectIdentifier("1.3.6.1.5.5.7.2.2");
                        using (w.PushSequence())
                        {
                            w.WriteCharacterString(UniversalTagNumber.UTF8String, "Tento certifikát je testovací.");
                        }
                    }
                }
            }
            using (w.PushSequence())
            {
                w.WriteObjectIdentifier("2.23.140.1.2.2");
            }
        }
        return w.Encode();
    }

    /// <summary>An extension whose value is text, so the OCTET STRING must stay a leaf.</summary>
    private static byte[] OpaqueExtensionValue()
        => "plain text that is not DER"u8.ToArray();

    private static IEnumerable<(string, byte[])> Synthetic()
    {
        yield return ("cer_mixed_indefinite", WriteMixed(new AsnWriter(AsnEncodingRules.CER)));
        yield return ("der_mixed", WriteMixed(new AsnWriter(AsnEncodingRules.DER)));
    }

    /// <summary>
    /// Every universal type the library knows, context and application tags, nested OCTET STRING
    /// encapsulation and a large SET. In CER mode every constructed node uses indefinite length.
    /// </summary>
    private static byte[] WriteMixed(AsnWriter w)
    {
        var inner = new AsnWriter(AsnEncodingRules.DER);
        using (inner.PushSequence())
        {
            inner.WriteInteger(-129);
            inner.WriteBoolean(false);
            inner.WriteOctetString(new byte[] { 0x04, 0x01, 0x7F });
        }
        var innerBytes = inner.Encode();

        using (w.PushSequence())
        {
            w.WriteBoolean(true);
            w.WriteInteger(0);
            w.WriteInteger(long.MaxValue);
            w.WriteInteger(System.Numerics.BigInteger.Parse("-123456789012345678901234567890"));
            w.WriteBitString(new byte[] { 0xA5, 0xF0 }, 4);
            w.WriteOctetString("not-der"u8);
            w.WriteOctetString(innerBytes);
            w.WriteOctetString(ReadOnlySpan<byte>.Empty);
            w.WriteNull();
            w.WriteObjectIdentifier("1.2.840.113549.1.1.11");
            w.WriteObjectIdentifier("2.999.1234567");
            w.WriteEnumeratedValue(UniversalTagNumber.Boolean);
            w.WriteCharacterString(UniversalTagNumber.UTF8String, "Žluťoučký kůň 🐎");
            w.WriteCharacterString(UniversalTagNumber.NumericString, "0123 4567");
            w.WriteCharacterString(UniversalTagNumber.PrintableString, "Printable (ok) = yes?");
            w.WriteCharacterString(UniversalTagNumber.IA5String, "ia5@example.test");
            w.WriteCharacterString(UniversalTagNumber.VisibleString, "visible");
            w.WriteCharacterString(UniversalTagNumber.BMPString, "BMP Ωmega");
            w.WriteUtcTime(new DateTimeOffset(2024, 2, 29, 23, 59, 58, TimeSpan.Zero));
            w.WriteGeneralizedTime(new DateTimeOffset(2099, 12, 31, 1, 2, 3, TimeSpan.Zero), omitFractionalSeconds: true);
            w.WriteInteger(7, new Asn1Tag(TagClass.ContextSpecific, 0));
            w.WriteOctetString([0xDE, 0xAD], new Asn1Tag(TagClass.ContextSpecific, 9));
            using (w.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3, isConstructed: true)))
            {
                w.WriteInteger(3);
            }
            using (w.PushSequence(new Asn1Tag(TagClass.Application, 5, isConstructed: true)))
            {
                w.WriteNull();
            }
            w.WriteInteger(9, new Asn1Tag(TagClass.Private, 2));
            using (w.PushSetOf())
            {
                for (int i = 0; i < 300; i++)
                    w.WriteInteger(i);
            }
            using (w.PushSequence())
            {
                using (w.PushSequence())
                {
                    using (w.PushSequence())
                    {
                        w.WriteCharacterString(UniversalTagNumber.UTF8String, "deep");
                    }
                }
            }
            if (w.RuleSet == AsnEncodingRules.DER)
            {
                // An empty indefinite SEQUENCE hits a known quirk (see KnownQuirkTests), so only DER gets one.
                using (w.PushSequence()) { }
            }
        }
        return w.Encode();
    }
}
