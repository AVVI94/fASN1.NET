using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using fASN1.NET.Tags;

namespace fASN1.NET.Tests;

public class SerializerTests
{
    [Fact]
    public void Deserialize_ShouldHandlePrimitiveTag()
    {
        // Arrange
        var stream = new MemoryStream([0x02, 0x01, 0x05]); // INTEGER 5
        var expectedTagNumber = 2; // INTEGER tag

        // Act
        var result = Asn1Serializer.Deserialize(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTagNumber, result.TagNumber);
        Assert.Equal(5, result.Content[0]);
    }

    [Fact]
    public void Deserialize_ShouldHandleConstructedTag()
    {
        // Arrange
        var stream = new MemoryStream([0x30, 0x03, 0x02, 0x01, 0x05]); // SEQUENCE { INTEGER 5 }
        var expectedTagNumber = 48; // SEQUENCE tag

        // Act
        var result = Asn1Serializer.Deserialize(stream);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Tags.Sequence>(result);
        Assert.Equal(expectedTagNumber, result.TagNumber);
        Assert.Single(result.Children);
        Assert.IsType<Tags.Integer>(result.Children[0]);
    }

    [Fact]
    public void Deserialize_ShouldReturnNullForEmptyStream()
    {
        // Arrange
        var stream = new MemoryStream();
        Assert.Throws<ArgumentException>(() => Asn1Serializer.Deserialize(stream));
    }

    [Theory]
    [InlineData("MIICwDCCAagCAQAwRTEVMBMGA1UEAwwMVGVzdCBSZXF1ZXN0MQswCQYDVQQGEwJDWjENMAsGA1UEKgwEVGVzdDEQMA4GA1UEBAwHUmVxdWVzdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALvVr36n19JAy+O2xMTsiOeBZ0Y6fDrYEi5VUBu/+3fW7GXeao927vU8kv5ryI007RuDfQARX8QVTg88ae8piq7WlrFTOyKX1sRvUp0mZNi1XetHi59JbUsIywc7XqV7e1q+kTvGGOhn6duiZ77dF6j3R8IL7dY7grFpy4sSysXlc0VI8Caa+e/upk8DZG3DIthAmhnoVwqH0wxeU4cOIIfNTr5YwpoXVMntWnwM9XnQceHgCcLQ0gs+WafYX0I/w1Q9DsjKTErzG21Vu0wlO1tzv2RuB765ECrmdH8lNylbUcD9PcSbcc2I5Y4BOhYYVNYELbu9PLJF8QoWpGMN3Q0CAwEAAaA2MDQGCSqGSIb3DQEJDjEnMCUwDgYDVR0PAQH/BAQDAgbAMBMGA1UdJQQMMAoGCCsGAQUFBwMEMA0GCSqGSIb3DQEBCwUAA4IBAQBOeUBFRfeBSP2DPqELxBLXzX7tFL/1usLQJO7RBryCPpjZJMetAX3xdQgu8YsJclugI4IepIq8z/fbInrL6dE7qsw0ZdLqTVJSOqRrzNuyTrRZDXyy8whTkCykoeVHopAKkwqNduOsIt3XlDQZE0/MK6yzT/d+e2YCeAUVzRrp3Su96ah7tKCC/thtbQjSZEpb6WMMPaAJjHjwqMW8xZF3hCx08DtTH8CGwwy+2G0ZCw5EYUG+OLDE3XOL78fGDjn0/ubeaC6dwqaKNHg1hz5fTjTd1yKBvYFM0217fPSfNRmFnrhlFdQ/5JTprgFAc84Q8mBBygXQEXotaEzjUFyV", "{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":2,\"TagName\":\"Integer\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"AA==\"},{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":49,\"TagName\":\"Set\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"VQQD\"},{\"TagNumber\":12,\"TagName\":\"UTF8String\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"VGVzdCBSZXF1ZXN0\"}],\"Content\":\"\"}],\"Content\":\"\"},{\"TagNumber\":49,\"TagName\":\"Set\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"VQQG\"},{\"TagNumber\":19,\"TagName\":\"PrintableString\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"Q1o=\"}],\"Content\":\"\"}],\"Content\":\"\"},{\"TagNumber\":49,\"TagName\":\"Set\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"VQQq\"},{\"TagNumber\":12,\"TagName\":\"UTF8String\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"VGVzdA==\"}],\"Content\":\"\"}],\"Content\":\"\"},{\"TagNumber\":49,\"TagName\":\"Set\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"VQQE\"},{\"TagNumber\":12,\"TagName\":\"UTF8String\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"UmVxdWVzdA==\"}],\"Content\":\"\"}],\"Content\":\"\"}],\"Content\":\"\"},{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"KoZIhvcNAQEB\"},{\"TagNumber\":5,\"TagName\":\"Null\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"\"}],\"Content\":\"\"},{\"TagNumber\":3,\"TagName\":\"BitString\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"ADCCAQoCggEBALvVr36n19JAy+O2xMTsiOeBZ0Y6fDrYEi5VUBu/+3fW7GXeao927vU8kv5ryI007RuDfQARX8QVTg88ae8piq7WlrFTOyKX1sRvUp0mZNi1XetHi59JbUsIywc7XqV7e1q+kTvGGOhn6duiZ77dF6j3R8IL7dY7grFpy4sSysXlc0VI8Caa+e/upk8DZG3DIthAmhnoVwqH0wxeU4cOIIfNTr5YwpoXVMntWnwM9XnQceHgCcLQ0gs+WafYX0I/w1Q9DsjKTErzG21Vu0wlO1tzv2RuB765ECrmdH8lNylbUcD9PcSbcc2I5Y4BOhYYVNYELbu9PLJF8QoWpGMN3Q0CAwEAAQ==\"}],\"Content\":\"\"},{\"TagNumber\":160,\"TagName\":\"[0]\",\"TagClass\":2,\"IsConstructed\":true,\"IsUniversal\":false,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"KoZIhvcNAQkO\"},{\"TagNumber\":49,\"TagName\":\"Set\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"VR0P\"},{\"TagNumber\":1,\"TagName\":\"Boolean\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"/w==\"},{\"TagNumber\":4,\"TagName\":\"OctetString\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":3,\"TagName\":\"BitString\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"BsA=\"}],\"Content\":\"\"}],\"Content\":\"\"},{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"VR0l\"},{\"TagNumber\":4,\"TagName\":\"OctetString\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"KwYBBQUHAwQ=\"}],\"Content\":\"\"}],\"Content\":\"\"}],\"Content\":\"\"}],\"Content\":\"\"}],\"Content\":\"\"}],\"Content\":\"\"}],\"Content\":\"\"}],\"Content\":\"\"},{\"TagNumber\":48,\"TagName\":\"Sequence\",\"TagClass\":0,\"IsConstructed\":true,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[{\"TagNumber\":6,\"TagName\":\"ObjectIdentifier\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"KoZIhvcNAQEL\"},{\"TagNumber\":5,\"TagName\":\"Null\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"\"}],\"Content\":\"\"},{\"TagNumber\":3,\"TagName\":\"BitString\",\"TagClass\":0,\"IsConstructed\":false,\"IsUniversal\":true,\"IsEoc\":false,\"Children\":[],\"Content\":\"AE55QEVF94FI/YM+oQvEEtfNfu0Uv/W6wtAk7tEGvII+mNkkx60BffF1CC7xiwlyW6Ajgh6kirzP99siesvp0TuqzDRl0upNUlI6pGvM27JOtFkNfLLzCFOQLKSh5UeikAqTCo1246wi3deUNBkTT8wrrLNP9357ZgJ4BRXNGundK73pqHu0oIL+2G1tCNJkSlvpYww9oAmMePCoxbzFkXeELHTwO1MfwIbDDL7YbRkLDkRhQb44sMTdc4vvx8YOOfT+5t5oLp3Cpoo0eDWHPl9ONN3XIoG9gUzTbXt89J81GYWeuGUV1D/klOmuAUBzzhDyYEHKBdARei1oTONQXJU=\"}],\"Content\":\"\"}")]
    public void Deserialize_ShouldDeserialize(string pem, string jsonResult)
    {
        var stream = new MemoryStream(Convert.FromBase64String(pem));
        var result = Asn1Serializer.Deserialize(stream);
        Assert.NotNull(result);
        Assert.True(result.IsCertificateRequest());
        var js = JsonSerializer.Serialize(result);
        Assert.Equal(jsonResult, js);
    }

    [Fact]
    public void Deserialize_ShouldDeserializeSkid()
    {
        var crt = X509Certificate2.CreateFromPem("""
                        -----BEGIN CERTIFICATE-----
            MIIGITCCBAmgAwIBAgIDAai2MA0GCSqGSIb3DQEBCwUAMH8xKDAmBgNVBAMMH0k
            uQ0EgVGVzdCBQdWJsaWMgQ0EvUlNBIDA0LzIwMjIxLTArBgNVBAoMJFBydm7DrS
            BjZXJ0aWZpa2HEjW7DrSBhdXRvcml0YSwgYS5zLjEXMBUGA1UEYQwOTlRSQ1otM
            jY0MzkzOTUxCzAJBgNVBAYTAkNaMB4XDTIzMDgxNjIwMjEwMVoXDTI0MDgxNTIw
            MjEwMVowgYMxDTALBgNVBAoMBHRlc3QxGDAWBgNVBGEMD05UUkNaLTEyMzQ1Njc
            5ODEmMCQGA1UECwwdU2x1xb5iYSBwZcSNZXTEm27DrSBuYSBkw6Fsa3UxDTALBg
            NVBAMMBHRlc3QxCzAJBgNVBAYTAkNaMRQwEgYDVQQFEwtJQ0EgLSA5NDAwNTCCA
            SIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAJrFOtoABCuuZvMYe2q9lgUV
            BH13z/rTJvX0X+eX3K+PLcCOPS2E5dY0R/6XDfn4Io3YcfJ+i+vbmgdqen0VXNO
            pDI48o7UZ0MTOQqZ11eqOg59E6Z1SSq2bo4DV7E8ymHTPAbJ5NMiUv4OIhSj2D7
            b0idsKfM//r4MWFH22cmIGQGw2de+ToDydEiinD+MqF+GU6Xcn4a7ZuQKi6AfZD
            oI6wzfuR3A+AK9tzPnlJCvA++SE/r2AqMKYqu2GJjYytlTEqw3ZTuVxtmVMHEyA
            udHE8EwaYRdF94XSkaDunQE0KlbcZlgBwxkLOcl1G4TPkdQIliMo+wGt9zYjqbj
            P+lkCAwEAAaOCAZ8wggGbMB8GCWCGSAGG+EIBDQQSFhA5MjAzNTUwMDAwMDAwNT
            I5MCAGA1UdEQQZMBegFQYKKwYBBAGBuEgEBqAHDAU5NDAwNTAOBgNVHQ8BAf8EB
            AMCBaAwRQYDVR0gBD4wPDAwBg0rBgEEAYG4SAoDRwEBMB8wHQYIKwYBBQUHAgEW
            EWh0dHA6Ly93d3cuaWNhLmN6MAgGBgQAj3oBATAzBgNVHR8ELDAqMCigJqAkhiJ
            odHRwOi8vdGVzdHMuaWNhLmN6L3RwY2EyMl9yc2EuY3JsMGoGCCsGAQUFBwEBBF
            4wXDAuBggrBgEFBQcwAoYiaHR0cDovL3Rlc3RzLmljYS5jei90cGNhMjJfcnNhL
            mNlcjAqBggrBgEFBQcwAYYeaHR0cDovL3RvY3NwLmljYS5jei90cGNhMjJfcnNh
            MAkGA1UdEwQCMAAwHwYDVR0jBBgwFoAUZaEc+pLhCvZMhevyM9phYkEEGeswHQY
            DVR0OBBYEFMonCu7U6eiz1U7KAa6q2KRfx91FMBMGA1UdJQQMMAoGCCsGAQUFBw
            MCMA0GCSqGSIb3DQEBCwUAA4ICAQAPdmZDIUFjXppfDP9TIJQH0JqoaVJ6aUhdg
            SzJ+sHv1UiQYO6zbRWdbEYDkIhV5d0PRd02N1pMQi9Q043lUHf0T5Y8NJ48FO2Y
            iX4enzi3A42VSvkcy/aSLtkxPw3vtEKsxpuQXOPFetqXKlWceheNJbV9wfccuUn
            YIovVP9KaGBmoz10dh2/kuVsqOhSpsOVMqB7Gp5/dbCIqt4RF3i0stlq4lfi4Ib
            nPpr+auZSdNfS62MoTLE0YABp5by4/f+85AcAJdvfYi7mq9GXNa/vKB3l6Uwlbm
            /hLzGGe0kpomilBbf+4UqvWxrggFaH1EETZACv2EkFAeDRLgzJxsm+sUjWw81Bq
            kiiGlQhZVLVDGwIKMf6Ja5hYfRGBUzs5nCt8foxWb+T5E1mOLlxuV1lJFbQjBXw
            LjVyuMmxV1DKxL/GxaojEX2NRaFhLeCsbeSV9GsUOdrIsUL8OOx5fOoag3s3bKm
            VWThuuk+VzwSEMoKUpvz8RiLTRjA50d5VasdLh5pnoLgODHfu3BaQQHupTr32iV
            XCDBXlZjEwkHNxEgJd0MqE1i+ZnyCL2RY9V9No2P2zv1oqqSyPwLszh/IfpHMsf
            DRyHmhcSNHumEZ+GXHG6VEWfPbO49BknP9VqiN2F9iKe1vkzUH0aB2hhJB06cOi
            /397nqcveDghQrsr+OQ==
            -----END CERTIFICATE-----
            """);
        var ext = crt.Extensions["2.5.29.14"];
        var data = Asn1Serializer.Deserialize(ext!.RawData);
        Assert.NotNull(data);
        var str = Asn1Serializer.TagToString(data, x => x.IncludeTagNameForContentTags = false);
        Assert.Equal("CA270AEED4E9E8B3D54ECA01AEAAD8A45FC7DD45", str, true);
    }

    [Fact]
    public void Deserialize_ShouldDeserializeAkid()
    {
        var crt = X509Certificate2.CreateFromPem("""
                        -----BEGIN CERTIFICATE-----
            MIIGITCCBAmgAwIBAgIDAai2MA0GCSqGSIb3DQEBCwUAMH8xKDAmBgNVBAMMH0k
            uQ0EgVGVzdCBQdWJsaWMgQ0EvUlNBIDA0LzIwMjIxLTArBgNVBAoMJFBydm7DrS
            BjZXJ0aWZpa2HEjW7DrSBhdXRvcml0YSwgYS5zLjEXMBUGA1UEYQwOTlRSQ1otM
            jY0MzkzOTUxCzAJBgNVBAYTAkNaMB4XDTIzMDgxNjIwMjEwMVoXDTI0MDgxNTIw
            MjEwMVowgYMxDTALBgNVBAoMBHRlc3QxGDAWBgNVBGEMD05UUkNaLTEyMzQ1Njc
            5ODEmMCQGA1UECwwdU2x1xb5iYSBwZcSNZXTEm27DrSBuYSBkw6Fsa3UxDTALBg
            NVBAMMBHRlc3QxCzAJBgNVBAYTAkNaMRQwEgYDVQQFEwtJQ0EgLSA5NDAwNTCCA
            SIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAJrFOtoABCuuZvMYe2q9lgUV
            BH13z/rTJvX0X+eX3K+PLcCOPS2E5dY0R/6XDfn4Io3YcfJ+i+vbmgdqen0VXNO
            pDI48o7UZ0MTOQqZ11eqOg59E6Z1SSq2bo4DV7E8ymHTPAbJ5NMiUv4OIhSj2D7
            b0idsKfM//r4MWFH22cmIGQGw2de+ToDydEiinD+MqF+GU6Xcn4a7ZuQKi6AfZD
            oI6wzfuR3A+AK9tzPnlJCvA++SE/r2AqMKYqu2GJjYytlTEqw3ZTuVxtmVMHEyA
            udHE8EwaYRdF94XSkaDunQE0KlbcZlgBwxkLOcl1G4TPkdQIliMo+wGt9zYjqbj
            P+lkCAwEAAaOCAZ8wggGbMB8GCWCGSAGG+EIBDQQSFhA5MjAzNTUwMDAwMDAwNT
            I5MCAGA1UdEQQZMBegFQYKKwYBBAGBuEgEBqAHDAU5NDAwNTAOBgNVHQ8BAf8EB
            AMCBaAwRQYDVR0gBD4wPDAwBg0rBgEEAYG4SAoDRwEBMB8wHQYIKwYBBQUHAgEW
            EWh0dHA6Ly93d3cuaWNhLmN6MAgGBgQAj3oBATAzBgNVHR8ELDAqMCigJqAkhiJ
            odHRwOi8vdGVzdHMuaWNhLmN6L3RwY2EyMl9yc2EuY3JsMGoGCCsGAQUFBwEBBF
            4wXDAuBggrBgEFBQcwAoYiaHR0cDovL3Rlc3RzLmljYS5jei90cGNhMjJfcnNhL
            mNlcjAqBggrBgEFBQcwAYYeaHR0cDovL3RvY3NwLmljYS5jei90cGNhMjJfcnNh
            MAkGA1UdEwQCMAAwHwYDVR0jBBgwFoAUZaEc+pLhCvZMhevyM9phYkEEGeswHQY
            DVR0OBBYEFMonCu7U6eiz1U7KAa6q2KRfx91FMBMGA1UdJQQMMAoGCCsGAQUFBw
            MCMA0GCSqGSIb3DQEBCwUAA4ICAQAPdmZDIUFjXppfDP9TIJQH0JqoaVJ6aUhdg
            SzJ+sHv1UiQYO6zbRWdbEYDkIhV5d0PRd02N1pMQi9Q043lUHf0T5Y8NJ48FO2Y
            iX4enzi3A42VSvkcy/aSLtkxPw3vtEKsxpuQXOPFetqXKlWceheNJbV9wfccuUn
            YIovVP9KaGBmoz10dh2/kuVsqOhSpsOVMqB7Gp5/dbCIqt4RF3i0stlq4lfi4Ib
            nPpr+auZSdNfS62MoTLE0YABp5by4/f+85AcAJdvfYi7mq9GXNa/vKB3l6Uwlbm
            /hLzGGe0kpomilBbf+4UqvWxrggFaH1EETZACv2EkFAeDRLgzJxsm+sUjWw81Bq
            kiiGlQhZVLVDGwIKMf6Ja5hYfRGBUzs5nCt8foxWb+T5E1mOLlxuV1lJFbQjBXw
            LjVyuMmxV1DKxL/GxaojEX2NRaFhLeCsbeSV9GsUOdrIsUL8OOx5fOoag3s3bKm
            VWThuuk+VzwSEMoKUpvz8RiLTRjA50d5VasdLh5pnoLgODHfu3BaQQHupTr32iV
            XCDBXlZjEwkHNxEgJd0MqE1i+ZnyCL2RY9V9No2P2zv1oqqSyPwLszh/IfpHMsf
            DRyHmhcSNHumEZ+GXHG6VEWfPbO49BknP9VqiN2F9iKe1vkzUH0aB2hhJB06cOi
            /397nqcveDghQrsr+OQ==
            -----END CERTIFICATE-----
            """);
        var ext = crt.Extensions["2.5.29.35"];
        var fuckme = ext.Format(true);
        var data = Asn1Serializer.Deserialize(ext!.RawData);
        Assert.NotNull(data);
        var str = Asn1Serializer.TagToString(data.Children[0], x => x.IncludeTagNameForContentTags = false);
        Assert.Equal("65A11CFA92E10AF64C85EBF233DA6162410419EB", str, true);
    }

    [Fact]
    public void Serialize_ShouldHandlePrimitiveTag()
    {
        // Arrange
        var tag = new Tags.Integer([0x05]);

        // Act
        var result = Asn1Serializer.Serialize(tag);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal(0x02, result[0]);
        Assert.Equal(0x01, result[1]);
        Assert.Equal(0x05, result[2]);
    }

    [Fact]
    public void Serialize_ShouldHandleConstructedTag()
    {
        // Arrange
        var tag = new Sequence([new Integer([0x05])]);

        // Act
        var result = Asn1Serializer.Serialize(tag);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Length);
        Assert.Equal(0x30, result[0]);
        Assert.Equal(0x03, result[1]);
        Assert.Equal(0x02, result[2]);
        Assert.Equal(0x01, result[3]);
        Assert.Equal(0x05, result[4]);
    }

    [Fact]
    public void Serialize_ShouldHandleEmptyTag()
    {
        // Arrange
        var tag = new Sequence();

        // Act
        var result = Asn1Serializer.Serialize(tag);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(0x30, result[0]);
        Assert.Equal(0x00, result[1]);
    }

    [Fact]
    public void Serialize_ShouldHandleEmptyConstructedTag()
    {
        // Arrange
        var tag = new Sequence([new Sequence()]);

        // Act
        var result = Asn1Serializer.Serialize(tag);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Length);
        Assert.Equal(0x30, result[0]);
        Assert.Equal(0x02, result[1]);
        Assert.Equal(0x30, result[2]);
        Assert.Equal(0x00, result[3]);
    }

    [Theory]
    [InlineData("MIICwDCCAagCAQAwRTEVMBMGA1UEAwwMVGVzdCBSZXF1ZXN0MQswCQYDVQQGEwJDWjENMAsGA1UEKgwEVGVzdDEQMA4GA1UEBAwHUmVxdWVzdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALvVr36n19JAy+O2xMTsiOeBZ0Y6fDrYEi5VUBu/+3fW7GXeao927vU8kv5ryI007RuDfQARX8QVTg88ae8piq7WlrFTOyKX1sRvUp0mZNi1XetHi59JbUsIywc7XqV7e1q+kTvGGOhn6duiZ77dF6j3R8IL7dY7grFpy4sSysXlc0VI8Caa+e/upk8DZG3DIthAmhnoVwqH0wxeU4cOIIfNTr5YwpoXVMntWnwM9XnQceHgCcLQ0gs+WafYX0I/w1Q9DsjKTErzG21Vu0wlO1tzv2RuB765ECrmdH8lNylbUcD9PcSbcc2I5Y4BOhYYVNYELbu9PLJF8QoWpGMN3Q0CAwEAAaA2MDQGCSqGSIb3DQEJDjEnMCUwDgYDVR0PAQH/BAQDAgbAMBMGA1UdJQQMMAoGCCsGAQUFBwMEMA0GCSqGSIb3DQEBCwUAA4IBAQBOeUBFRfeBSP2DPqELxBLXzX7tFL/1usLQJO7RBryCPpjZJMetAX3xdQgu8YsJclugI4IepIq8z/fbInrL6dE7qsw0ZdLqTVJSOqRrzNuyTrRZDXyy8whTkCykoeVHopAKkwqNduOsIt3XlDQZE0/MK6yzT/d+e2YCeAUVzRrp3Su96ah7tKCC/thtbQjSZEpb6WMMPaAJjHjwqMW8xZF3hCx08DtTH8CGwwy+2G0ZCw5EYUG+OLDE3XOL78fGDjn0/ubeaC6dwqaKNHg1hz5fTjTd1yKBvYFM0217fPSfNRmFnrhlFdQ/5JTprgFAc84Q8mBBygXQEXotaEzjUFyV")]
    public void Serialize_SerializedDeserializedInput_ShouldBeEqual(string pem)
    {
        var stream = new MemoryStream(Convert.FromBase64String(pem));
        var tag = Asn1Serializer.Deserialize(stream);
        Assert.NotNull(tag);
        var serialized = Asn1Serializer.Serialize(tag);
        var serPem = Convert.ToBase64String(serialized);
        Assert.Equal(pem, serPem);
    }

    [Fact]
    public void TagToString_ShouldHandleTruncate()
    {
        // Arrange
        var tag = new Utf8String(Encoding.UTF8.GetBytes("This is a very long content that should be truncated."));

        var options = new Asn1StringSerializerOptions
        {
            MaximumContentLineLength = 20,
            ContentLengthHandling = ContentLengthHandling.Truncate,
            TruncateString = "..."
        };

        // Act
        var result = Asn1Serializer.TagToString(tag, options);

        // Assert
        Assert.Contains("This is a very lo...", result);
    }

    [Fact]
    public void TagToString_ShouldHandleWrap()
    {
        // Arrange
        var tag = new MockTag
        {
            TagNumber = (int)Tag.UTF8String,
            TagName = "TestTag",
            Content = Encoding.UTF8.GetBytes("This is a very long content that should be wrapped."),
            Children = new List<ITag>()
        };

        var options = new Asn1StringSerializerOptions
        {
            MaximumContentLineLength = 22,
            ContentLengthHandling = ContentLengthHandling.Wrap,
            IndentationString = "",
            IncludeTagNameForContentTags = false
        };

        // Act
        var result = Asn1Serializer.TagToString(tag, options);

        // 
        Assert.StartsWith(Environment.NewLine + "This is a very long co", result);
        Assert.Contains("ntent that should be w", result);
        Assert.Contains("rapped.", result);
    }

    [Fact]
    public void TagToString_ShouldHandleWordWrap()
    {
        // Arrange
        var tag = new MockTag
        {
            TagNumber = (int)Tag.UTF8String,
            TagName = "TestTag",
            Content = Encoding.UTF8.GetBytes("This is a very long content that should be word wrapped."),
            Children = new List<ITag>()
        };

        var options = new Asn1StringSerializerOptions
        {
            MaximumContentLineLength = 20,
            ContentLengthHandling = ContentLengthHandling.WordWrap,
            IndentationString = "",
            IncludeTagNameForContentTags = false
        };

        // Act
        var result = Asn1Serializer.TagToString(tag, options);

        // Assert
        Assert.StartsWith(Environment.NewLine + "This is a very long", result);
        Assert.Contains("content that should", result);
        Assert.EndsWith("be word wrapped.", result);
    }

    [Fact]
    public void TagToString_ShouldIncludeTagName()
    {
        // Arrange
        var tag = new MockTag
        {
            TagNumber = (int)Tag.UTF8String,
            TagName = "TestTag",
            Content = Encoding.UTF8.GetBytes("Content"),
            Children = new List<ITag>()
        };

        var options = new Asn1StringSerializerOptions
        {
            IncludeTagNameForContentTags = true
        };

        // Act
        var result = Asn1Serializer.TagToString(tag, options);

        // Assert
        Assert.Contains("TestTag", result);
    }

    [Fact]
    public void TagToString_ShouldSerializerCertificate()
    {
        var pem = "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";
        var stream = new MemoryStream(Convert.FromBase64String(pem));
        var tag = Asn1Serializer.Deserialize(stream);
        Assert.NotNull(tag);
        var serialized = Asn1Serializer.TagToString(tag, new Asn1StringSerializerOptions()
        {
            ContentLengthHandling = ContentLengthHandling.WordWrap,
            IncludeTagNameForContentTags = true,
        });
        Assert.NotNull(serialized);

    }

    [Fact]
    public void TagToBytes_ShouldSerializeExactlyAsWas()
    {
        var pem = "MIIC9jCCAd4CAQAwYzEUMBIGA1UEAwwLdGVzdC5pY2EuY3oxDjAMBgNVBAcMBVByYWhhMQswCQYDVQQGEwJDWjERMA8GA1UECgwIT3JnIGEucy4xCzAJBgNVBAkMAjQyMQ4wDAYDVQQRDAUzMDAwMTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMnsxv1KewfQ3GBw9nxED/aM2JTFMf2i01PbB8L2ugdwyY0vei70k0fundw/s+ZLSGU7EoONyFUe+bftm83p/PsYsq4+ANlbRSAd1eTp0RF3A5hdvc95rscthVt+RAzOsjRrisLCZ9+QwystsAonLcOIktd7F9fZbw9DE7Qjwsl+fQMJK5fJrLMC/kso+6R2sl0LEnLEtxjzoOwWHkRwDm8Fq84m5eu9vW2KIF3maH1K59pm+D6EgyALZvB3dYESzV4HAfXQl1HlkQXDORqcm1/z2r3OCzaCy+2KD0EojEw8D9vujs+NzhIxhRAQPIWMgetIQgzuW11suXJpg+vDYdkCAwEAAaBOMEwGCSqGSIb3DQEJDjE/MD0wFgYDVR0RBA8wDYILdGVzdC5pY2EuY3owDgYDVR0PAQH/BAQDAgWgMBMGA1UdJQQMMAoGCCsGAQUFBwMBMA0GCSqGSIb3DQEBCwUAA4IBAQAMSK/VccjUr/6xIQWUVyWj0zb9UeUYsY2pfpeDuWTKvHpOy4MqBrKjdCS7Md26grBhkHoMvCT1QVp16tGJ4UmvjSI5yTUeW/5OQGOaz3quGHAl35bclEbBx+nIx1pBPZckaHLU38tB4Q4J4ikUx/seqU1h3+K5vmdAyv+VLDX0CuOF3wO8xQk2cwKm1RDUklt7C9AmsTe3IgndmJCSKs86vIO2l0r2wpCFWrO+zE1f9Ag9gPYC66relqxf4UISTdKfgBrfp703SVSfGl5o69SEhURR/9wJjR6yvknxCG4+fG1lfySqioEHVLwXQHk9OKQVZ0Tt5GjL41V7EdLBJJGd";
        var stream = new MemoryStream(Convert.FromBase64String(pem));
        var tag = Asn1Serializer.Deserialize(stream);
        Assert.NotNull(tag);
        var serialized = Asn1Serializer.Serialize(tag);
        Assert.NotNull(serialized);
        var pem2 = Convert.ToBase64String(serialized);
        Console.WriteLine(pem2);
        Assert.Equal(pem2, pem);
    }
}
// Mock implementation of ITag for testing purposes
public class MockTag : ITag
{
    public int TagNumber { get; set; }
    public string TagName { get; set; }
    public byte[] Content { get; set; }
    public IList<ITag> Children { get; set; }
    public int TagClass { get; }
    public bool IsConstructed { get; }
    public bool IsUniversal { get; }
    public bool IsEoc { get; }
}