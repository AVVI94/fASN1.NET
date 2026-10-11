using System.Security.Cryptography.Pkcs;
using System.Text;
using fASN1.NET.Tags;
using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// A real I.CA renewal request in CMS SignedData, BER-encoded with indefinite lengths and the XML payload in a
/// constructed OCTET STRING, with every personal value replaced by a fake of the same length (so the signature no
/// longer verifies). Version 1.4.3 returned 11 of its 232 nodes and reported success.
/// </summary>
public class CmsRegressionTests
{
    private const string NAME = "file:cms_signed_ber_renewal";

    [Fact]
    public void RenewalRequest_ParsesCompletely()
    {
        var tag = Asn1Serializer.Deserialize(TestCorpus.Get(NAME));
        var nodes = TreeAssert.Walk(tag).Select(n => n.Tag).ToList();

        Assert.Equal(232, nodes.Count);
        Assert.DoesNotContain(nodes, n => n is Unknown);

        // ContentInfo { contentType, [0] SignedData { version, digestAlgorithms, encapContentInfo, [0] certificates, signerInfos } }
        var signedData = tag.Children[1].Children[0];
        Assert.Equal(5, signedData.Children.Count);

        var payload = Assert.IsType<OctetString>(signedData.Children[2].Children[1].Children[0]);
        Assert.Equal(1815, payload.Content.Length); // joined from fragments of 1000 and 815 octets
        var xml = Encoding.UTF8.GetString(payload.Content);
        Assert.StartsWith("<?xml", xml);
        Assert.Contains("<Email>novak@test.cz</Email>", xml);

        var certificate = signedData.Children[3].Children[0];
        Assert.True(certificate.IsCertificate());
        Assert.True(certificate.TryGetCertificateSubjectItem(SubjectItemKind.CommonName, false, out var cn));
        Assert.Equal(["Karel Kučera"], cn);
        Assert.True(certificate.TryGetCertificateSerialNumber(out var serial));
        Assert.Equal("70000000000000000001", serial);
    }

    [Fact]
    public void RenewalRequest_MatchesDotNetSignedCms_BeforeAndAfterSerialize()
    {
        var data = TestCorpus.Get(NAME);
        var tag = Asn1Serializer.Deserialize(data);
        var payload = tag.Children[1].Children[0].Children[2].Children[1].Children[0].Content;

        foreach (var encoding in new[] { data, Asn1Serializer.Serialize(tag) })
        {
            var cms = new SignedCms();
            cms.Decode(encoding);

            Assert.Equal(payload, cms.ContentInfo.Content);
            Assert.Equal("CN=Karel Kučera", cms.Certificates[0].Subject.Split(", ").Last());
            Assert.Single(cms.SignerInfos);
        }
    }
}
