using BenchmarkDotNet.Attributes;
using fASN1.NET.Tags;

namespace fASN1.NET.Benchmark;

#nullable disable
[MemoryDiagnoser]
[MarkdownExporter]
public class Bench
{
    byte[] _certData;
    byte[] _certReqData;

    ITag _certTag;
    ITag _certReqTag;

    [GlobalSetup]
    public void Setup()
    {
        _certData = Convert.FromBase64String("MIIEozCCBEmgAwIBAgIQTij3hrZsGjuULNLEDrdCpTAKBggqhkjOPQQDAjCBjzELMAkGA1UEBhMCR0IxGzAZBgNVBAgTEkdyZWF0ZXIgTWFuY2hlc3RlcjEQMA4GA1UEBxMHU2FsZm9yZDEYMBYGA1UEChMPU2VjdGlnbyBMaW1pdGVkMTcwNQYDVQQDEy5TZWN0aWdvIEVDQyBEb21haW4gVmFsaWRhdGlvbiBTZWN1cmUgU2VydmVyIENBMB4XDTI0MDMwNzAwMDAwMFoXDTI1MDMwNzIzNTk1OVowFTETMBEGA1UEAxMKZ2l0aHViLmNvbTBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABARO/Ho9XdkY1qh9mAgjOUkWmXTb05jgRulKciMVBuKB3ZHexvCdyoiCRHEMBfFXoZhWkQVMogNLo/lW215X3pGjggL+MIIC+jAfBgNVHSMEGDAWgBT2hQo7EYbhBH0Oqgss0u7MZHt7rjAdBgNVHQ4EFgQUO2g/NDr1RzTK76ZOPZq9Xm56zJ8wDgYDVR0PAQH/BAQDAgeAMAwGA1UdEwEB/wQCMAAwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMEkGA1UdIARCMEAwNAYLKwYBBAGyMQECAgcwJTAjBggrBgEFBQcCARYXaHR0cHM6Ly9zZWN0aWdvLmNvbS9DUFMwCAYGZ4EMAQIBMIGEBggrBgEFBQcBAQR4MHYwTwYIKwYBBQUHMAKGQ2h0dHA6Ly9jcnQuc2VjdGlnby5jb20vU2VjdGlnb0VDQ0RvbWFpblZhbGlkYXRpb25TZWN1cmVTZXJ2ZXJDQS5jcnQwIwYIKwYBBQUHMAGGF2h0dHA6Ly9vY3NwLnNlY3RpZ28uY29tMIIBgAYKKwYBBAHWeQIEAgSCAXAEggFsAWoAdwDPEVbu1S58r/OHW9lpLpvpGnFnSrAX7KwB0lt3zsw7CAAAAY4WOvAZAAAEAwBIMEYCIQD7oNz/2oO8VGaWWrqrsBQBzQH0hRhMLm11oeMpg1fNawIhAKWc0q7Z+mxDVYV/6ov7f/i0H/aAcHSCIi/QJcECraOpAHYAouMK5EXvva2bfjjtR2d3U9eCW4SU1yteGyzEuVCkR+cAAAGOFjrv+AAABAMARzBFAiEAyupEIVAMk0c8BVVpF0QbisfoEwy5xJQKQOe8EvMU4W8CIGAIIuzjxBFlHpkqcsa7UZy24y/B6xZnktUw/Ne5q5hCAHcATnWjJ1yaEMM4W2zU3z9S6x3w4I4bjWnAsfpksWKaOd8AAAGOFjrv9wAABAMASDBGAiEA+8OvQzpgRf31uLBsCE8ktCUfvsiRT7zWSqeXliA09TUCIQDcB7Xn97aEDMBKXIbdm5KZ9GjvRyoF9skD5/4GneoMWzAlBgNVHREEHjAcggpnaXRodWIuY29tgg53d3cuZ2l0aHViLmNvbTAKBggqhkjOPQQDAgNIADBFAiEAru2McPr0eNwcWNuDEY0a/rGzXRfRrm+6XfZeSzhYZewCIBq4TUEBCgapv7xvAtRKdVdi/b4m36Uyej1ggyJsiesA");
        _certReqData = Convert.FromBase64String("MIIEpjCCAo4CAQAwQDENMAsGA1UEAwwEdGVzdDENMAsGA1UECgwEdGVzdDETMBEGA1UEYQwKTlRSQ1otdGVzdDELMAkGA1UEBhMCQ1owggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQCtvOZ35i4i/Lt6bnb2AR+ni0LN08qM1OxAH2hcLHLrj4cjLPBuaH+RKusFxYOGw/oRTmT9Tk8n8nd6y69yxef4wwz65sC6iYQgDns6ngjLC9YIqtv+dbY0SoUh5FY7oQZ5Ra2N2E9in/tHg1xsaQji8/hWa/jP4oXegj8NXxhg+I4S4XL0wcUYKR+GfBnxlCEqrGvCqIwKww1PqRdpjeJlnMFNQEgYDyG8+JnAjTnMxiz7+eeqxeiIYTC4PcuhrhlzcownyUJX3jZZdAELIQPOVgZIrkGRZh+aSYdwP8NEtBy1WzB38R5QJsXj8IcJW2nkh2J19X5nd2lNcKVqWinnuPqBrfUOaZsQdqTvObzwpXw9UzL+BaTyQuyJ9RkqpnqYB8D8DFnxDMdtA20Ae38mniYX4GrZc8+6CZEp/1mJwpK9TtsEK7/YH5+tLMkE2/CG6/zcCk731koZVwGlk2o8S41AFSgfZuOYl+o3s9GD3/p9KPw8fWFySSulyEXO8X9K6+ySyHdQETBY7E3TgvOJefDMbTuiQGRg9KuNocpvetnpDQQ/q2rkDfRGUv3bptfgjhKNZ6CYCEFHi4/xqx7akDo91egB9XPpWPsr4v+8jP2UeTrYIqb+TGCQG9T+jjDed+530z7RkyWc0yUJFmVMyu2Sa8+hXpyIn/bHO/i2AwIDAQABoCEwHwYJKoZIhvcNAQkOMRIwEDAOBgNVHQ8BAf8EBAMCBSAwDQYJKoZIhvcNAQEKBQADggIBACHfOaAJ9bulbeauk7Es7sD/PifShi12WUGvCL3zTPDUcgK/CozJle6Kcud4k+xqGxYtqrkboah56EHExhQB85dnvkQyDsUra1KqksnM2Yydbma7cr8qWvFOADL/KUsMG64mH4TpKm/7MIU09b5HEBh0/Lu10Xj2obHBEHq3uRCkZzum2HOMaicb3XhJnZaI+V8YMnbbyU0XPkR4hnsIUvID3y/XIbKV18SHl0LWTE8UOlT9cjrG3b/Su7ZHr/2b/qb0dpteyf3u7Nv7ZohjI8VJ/hmezUAdWQM+eEmhcnVlmgQj25UxAiuHmTYLiIuaJL76DBmQZf66Kdngr+u1NkoARMJvZRbJpQ32u0giAHhCu2ZxY1sLcLEoL8+OIRbqFk2dNJwsZmUkCY7KdKhte7AFj8qJfEajH/pMoR04Kgr6OThM9I0FuFy6VkcazRWn/O5doYkzltlsLk7hFw8hBrL0E/UcKnkP33TZvqHPtQhNT/05Ne0BrAynYg6yKiO1uWJVrWM1pGis9ZmR7FjRc7wXgdLHkraDr0WNHZLv0+OXEZpm3YWtgCJnid0sBh5unu0GiYq9+o2UAaKEj3T38YZgEqPsPQ50HjA/MbnHungeiIOg2ph/4iZ0PQ/ACEJ82f2eMoWxbBM9SiCKC1+EpC7s2t1LiFhertTE7JRQXaR8");
        using var ms = new MemoryStream(_certData);
        _certTag = fASN1.NET.Asn1Serializer.Deserialize(ms);

        using var ms2 = new MemoryStream(_certReqData);
        _certReqTag = fASN1.NET.Asn1Serializer.Deserialize(ms2);
    }

    //[Benchmark]
    //public ITag DeserializeCertificate()
    //{
    //    using var ms = new MemoryStream(_certData);
    //    return fASN1.NET.Asn1Serializer.Deserialize(ms);
    //}

    //[Benchmark]
    //public ITag DeserializeCertificateRequest()
    //{
    //    using var ms = new MemoryStream(_certReqData);
    //    return fASN1.NET.Asn1Serializer.Deserialize(ms, out _);
    //}

    //[Benchmark]
    //public byte[] SerializeCertificate()
    //{
    //    return fASN1.NET.Asn1Serializer.Serialize(_certTag);
    //}

    //[Benchmark]
    //public byte[] SerializeCertificateRequest()
    //{
    //    return fASN1.NET.Asn1Serializer.Serialize(_certReqTag);
    //}

    [Benchmark]
    public string SerializeCertificateTagToString()
    {
        return Asn1Serializer.TagToString(_certTag);
    }

    //[Benchmark]
    //public string SerializeCertificateRequestTagToString()
    //{
    //    return Asn1Serializer.TagToString(_certReqTag);
    //}
}
