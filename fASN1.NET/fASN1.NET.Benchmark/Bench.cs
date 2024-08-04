using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        _certData = Convert.FromBase64String("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");
        _certReqData = Convert.FromBase64String("MIIEpjCCAo4CAQAwQDENMAsGA1UEAwwEdGVzdDENMAsGA1UECgwEdGVzdDETMBEGA1UEYQwKTlRSQ1otdGVzdDELMAkGA1UEBhMCQ1owggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQCtvOZ35i4i/Lt6bnb2AR+ni0LN08qM1OxAH2hcLHLrj4cjLPBuaH+RKusFxYOGw/oRTmT9Tk8n8nd6y69yxef4wwz65sC6iYQgDns6ngjLC9YIqtv+dbY0SoUh5FY7oQZ5Ra2N2E9in/tHg1xsaQji8/hWa/jP4oXegj8NXxhg+I4S4XL0wcUYKR+GfBnxlCEqrGvCqIwKww1PqRdpjeJlnMFNQEgYDyG8+JnAjTnMxiz7+eeqxeiIYTC4PcuhrhlzcownyUJX3jZZdAELIQPOVgZIrkGRZh+aSYdwP8NEtBy1WzB38R5QJsXj8IcJW2nkh2J19X5nd2lNcKVqWinnuPqBrfUOaZsQdqTvObzwpXw9UzL+BaTyQuyJ9RkqpnqYB8D8DFnxDMdtA20Ae38mniYX4GrZc8+6CZEp/1mJwpK9TtsEK7/YH5+tLMkE2/CG6/zcCk731koZVwGlk2o8S41AFSgfZuOYl+o3s9GD3/p9KPw8fWFySSulyEXO8X9K6+ySyHdQETBY7E3TgvOJefDMbTuiQGRg9KuNocpvetnpDQQ/q2rkDfRGUv3bptfgjhKNZ6CYCEFHi4/xqx7akDo91egB9XPpWPsr4v+8jP2UeTrYIqb+TGCQG9T+jjDed+530z7RkyWc0yUJFmVMyu2Sa8+hXpyIn/bHO/i2AwIDAQABoCEwHwYJKoZIhvcNAQkOMRIwEDAOBgNVHQ8BAf8EBAMCBSAwDQYJKoZIhvcNAQEKBQADggIBACHfOaAJ9bulbeauk7Es7sD/PifShi12WUGvCL3zTPDUcgK/CozJle6Kcud4k+xqGxYtqrkboah56EHExhQB85dnvkQyDsUra1KqksnM2Yydbma7cr8qWvFOADL/KUsMG64mH4TpKm/7MIU09b5HEBh0/Lu10Xj2obHBEHq3uRCkZzum2HOMaicb3XhJnZaI+V8YMnbbyU0XPkR4hnsIUvID3y/XIbKV18SHl0LWTE8UOlT9cjrG3b/Su7ZHr/2b/qb0dpteyf3u7Nv7ZohjI8VJ/hmezUAdWQM+eEmhcnVlmgQj25UxAiuHmTYLiIuaJL76DBmQZf66Kdngr+u1NkoARMJvZRbJpQ32u0giAHhCu2ZxY1sLcLEoL8+OIRbqFk2dNJwsZmUkCY7KdKhte7AFj8qJfEajH/pMoR04Kgr6OThM9I0FuFy6VkcazRWn/O5doYkzltlsLk7hFw8hBrL0E/UcKnkP33TZvqHPtQhNT/05Ne0BrAynYg6yKiO1uWJVrWM1pGis9ZmR7FjRc7wXgdLHkraDr0WNHZLv0+OXEZpm3YWtgCJnid0sBh5unu0GiYq9+o2UAaKEj3T38YZgEqPsPQ50HjA/MbnHungeiIOg2ph/4iZ0PQ/ACEJ82f2eMoWxbBM9SiCKC1+EpC7s2t1LiFhertTE7JRQXaR8");
        using var ms = new MemoryStream(_certData);
        _certTag = fASN1.NET.Asn1Serializer.Deserialize(ms, out _);

        using var ms2 = new MemoryStream(_certReqData);
        _certReqTag = fASN1.NET.Asn1Serializer.Deserialize(ms2, out _);
    }

    //[Benchmark]
    //public ITag DeserializeCertificate()
    //{
    //    using var ms = new MemoryStream(_certData);
    //    return fASN1.NET.Asn1Serializer.Deserialize(ms, out _);
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
        return Asn1Serializer.TagToString(_certTag, x =>
        {
        });
    }

    //[Benchmark]
    //public string SerializeCertificateRequestTagToString()
    //{
    //    return Asn1Serializer.TagToString(_certReqTag);
    //}
}
