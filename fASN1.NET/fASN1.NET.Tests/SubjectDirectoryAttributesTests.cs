using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using fASN1.NET.SubjectDirectory;

namespace fASN1.NET.Tests;
public class SubjectDirectoryAttributesTests
{
    static readonly byte[] _pem = Convert.FromBase64String("MIIDEDCCAnmgAwIBAgIESZYC0jANBgkqhkiG9w0BAQUFADBIMQswCQYDVQQGEwJERTE5MDcGA1UECgwwR01EIC0gRm9yc2NodW5nc3plbnRydW0gSW5mb3JtYXRpb25zdGVjaG5payBHbWJIMB4XDTA0MDIwMTEwMDAwMFoXDTA4MDIwMTEwMDAwMFowZTELMAkGA1UEBhMCREUxNzA1BgNVBAoMLkdNRCBGb3JzY2h1bmdzemVudHJ1bSBJbmZvcm1hdGlvbnN0ZWNobmlrIEdtYkgxHTAMBgNVBCoMBVBldHJhMA0GA1UEBAwGQmFyemluMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDc50zVodVa6wHPXswg88P8p4fPy1caIaqKIK1d/wFRMN5yTl7T+VOS57sWxKcdDzGzqZJqjwjqAP3DqPK7AW3so7lBG6JZmiqMtlXG3+olv+3cc7WU+qDv5ZXGEqauW4x/DKGc7E/nq2BUZ2hLsjh9Xy9+vbw+8KYE9rQEARdpJQIDAQABo4HpMIHmMGQGA1UdCQRdMFswEAYIKwYBBQUHCQQxBBMCREUwDwYIKwYBBQUHCQMxAxMBRjAdBggrBgEFBQcJATERGA8xOTcxMTAxNDEyMDAwMFowFwYIKwYBBQUHCQIxCwwJRGFybXN0YWR0MA4GA1UdDwEB/wQEAwIGQDASBgNVHSAECzAJMAcGBSskCAEBMB8GA1UdIwQYMBaAFAABAgMEBQYHCAkKCwwNDg/+3LqYMDkGCCsGAQUFBwEDBC0wKzApBggrBgEFBQcLAjAdMBuBGW11bmljaXBhbGl0eUBkYXJtc3RhZHQuZGUwDQYJKoZIhvcNAQEFBQADgYEAj4yAu7LYa3X04h+C7+DyD2xViJCm5zEYg1m5x4znHJIMZsYAU/vJJIJQkPKVsIgm6vP/H1kXyAu0g2Epz+VWPnhZK1uw+ay1KRXw8rw2mR8hQ2Ug6QZHYdky2HH3H/69rWSPp888G8CW8RLUuIKzn+GhapCuGoC4qWdlGLWqfpc=");
    [Fact]
    public void TryGetSubjectDirectoryAttributesFromCertificate_ValidCertificate_ParsesSdaFromCert()
    {
        var tag = Asn1Serializer.Deserialize(_pem);
        var res = tag.TryGetSubjectDirectoryAttributesFromCertificate(out var sda);
        CommonAsserts(res, sda);
    }

    [Fact]
    public void FromCertificate_ValidCertificate_ParsesSdaFromCert()
    {
        var cert = new X509Certificate2(_pem);
        var sda = SubjectDirectoryAttributes.FromCertificate(cert);
        CommonAsserts(sda != null, sda);
    }

    private static void CommonAsserts(bool res, SubjectDirectoryAttributes? sda)
    {
        Assert.True(res);
        Assert.Equal("DE", Assert.Single(sda.CountryOfCitizenship));
        Assert.Equal("F", sda.GenderString);
        Assert.Equal(SubjectDirectory.Gender.Female, sda.Gender);
        Assert.Equal(new DateTime(1971, 10, 14).Date, sda.DateOfBirth?.Date);
        Assert.Equal("Darmstadt", sda.PlaceOfBirth);
    }

    /*
 |  |  |  |  |  |  | SEQUENCE
 |  |  |  |  |  |  |  | OBJECT_IDENTIFIER 1.3.6.1.5.5.7.9.4, countryOfCitizenship, PKIX personal data
 |  |  |  |  |  |  |  | SET
 |  |  |  |  |  |  |  |  | PrintableString DE
 |  |  |  |  |  |  | SEQUENCE
 |  |  |  |  |  |  |  | OBJECT_IDENTIFIER 1.3.6.1.5.5.7.9.3, gender, PKIX personal data
 |  |  |  |  |  |  |  | SET
 |  |  |  |  |  |  |  |  | PrintableString F
 |  |  |  |  |  |  | SEQUENCE
 |  |  |  |  |  |  |  | OBJECT_IDENTIFIER 1.3.6.1.5.5.7.9.1, dateOfBirth, PKIX personal data
 |  |  |  |  |  |  |  | SET
 |  |  |  |  |  |  |  |  | GeneralizedTime 1971/10/14 12:00:00 UTC
 |  |  |  |  |  |  | SEQUENCE
 |  |  |  |  |  |  |  | OBJECT_IDENTIFIER 1.3.6.1.5.5.7.9.2, placeOfBirth, PKIX personal data
 |  |  |  |  |  |  |  | SET
 |  |  |  |  |  |  |  |  | UTF8String Darmstadt
 |  |  |  | SEQUENCE
     */
}
