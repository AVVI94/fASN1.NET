# fASN1.NET

![Nuget](https://img.shields.io/nuget/v/fASN1.NET) ![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)

fASN1.NET is a .NET library for working with ASN.1 (Abstract Syntax Notation One) data. It provides functionality for serializing and deserializing ASN.1 data, as well as extracting various pieces of information from ASN.1 encoded certificates.

Features

* Serialize and deserialize ASN.1 data.
* ASN.1 data structured string representation.
* Extract key usage, extended key usage, SAN (Subject Alternative Name), and other certificate details.
* Support for long string content handling, including wrapping and word wrapping.

### ASN.1 references

* [Wikipedia ASN.1](https://en.wikipedia.org/wiki/Abstract_Syntax_Notation_One)
* [ITU-T X.690](https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf)
* [RFC 5280](https://datatracker.ietf.org/doc/html/rfc5280)
* [RFC 5912](https://datatracker.ietf.org/doc/html/rfc5912)
* [A Layman's Guide to a Subset of ASN.1, BER, and DER](https://luca.ntop.org/Teaching/Appunti/asn1.html)

## Installation

fASN1.NET is available as a NuGet package. You can install it using the following command:

```bash
dotnet add package fASN1.NET
```

## Usage

### Deserialize ASN.1 Data and get certificate details

The main purpose of the library is to work with ASN.1 encoded certificates. 
The following example demonstrates how to deserialize an ASN.1 encoded certificate and extract various pieces of information from it.

```cs
using System;
using System.Collections.Generic;
using System.IO;
using fASN1.NET;

//current github ssl cert (as of 04.08.2024)
var data = Convert.FromBase64String("MIIEozCCBEmgAwIBAgIQTij3hrZsGjuULNLEDrdCpTAKBggqhkjOPQQDAjCBjzELMAkGA1UEBhMCR0IxGzAZBgNVBAgTEkdyZWF0ZXIgTWFuY2hlc3RlcjEQMA4GA1UEBxMHU2FsZm9yZDEYMBYGA1UEChMPU2VjdGlnbyBMaW1pdGVkMTcwNQYDVQQDEy5TZWN0aWdvIEVDQyBEb21haW4gVmFsaWRhdGlvbiBTZWN1cmUgU2VydmVyIENBMB4XDTI0MDMwNzAwMDAwMFoXDTI1MDMwNzIzNTk1OVowFTETMBEGA1UEAxMKZ2l0aHViLmNvbTBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABARO/Ho9XdkY1qh9mAgjOUkWmXTb05jgRulKciMVBuKB3ZHexvCdyoiCRHEMBfFXoZhWkQVMogNLo/lW215X3pGjggL+MIIC+jAfBgNVHSMEGDAWgBT2hQo7EYbhBH0Oqgss0u7MZHt7rjAdBgNVHQ4EFgQUO2g/NDr1RzTK76ZOPZq9Xm56zJ8wDgYDVR0PAQH/BAQDAgeAMAwGA1UdEwEB/wQCMAAwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMEkGA1UdIARCMEAwNAYLKwYBBAGyMQECAgcwJTAjBggrBgEFBQcCARYXaHR0cHM6Ly9zZWN0aWdvLmNvbS9DUFMwCAYGZ4EMAQIBMIGEBggrBgEFBQcBAQR4MHYwTwYIKwYBBQUHMAKGQ2h0dHA6Ly9jcnQuc2VjdGlnby5jb20vU2VjdGlnb0VDQ0RvbWFpblZhbGlkYXRpb25TZWN1cmVTZXJ2ZXJDQS5jcnQwIwYIKwYBBQUHMAGGF2h0dHA6Ly9vY3NwLnNlY3RpZ28uY29tMIIBgAYKKwYBBAHWeQIEAgSCAXAEggFsAWoAdwDPEVbu1S58r/OHW9lpLpvpGnFnSrAX7KwB0lt3zsw7CAAAAY4WOvAZAAAEAwBIMEYCIQD7oNz/2oO8VGaWWrqrsBQBzQH0hRhMLm11oeMpg1fNawIhAKWc0q7Z+mxDVYV/6ov7f/i0H/aAcHSCIi/QJcECraOpAHYAouMK5EXvva2bfjjtR2d3U9eCW4SU1yteGyzEuVCkR+cAAAGOFjrv+AAABAMARzBFAiEAyupEIVAMk0c8BVVpF0QbisfoEwy5xJQKQOe8EvMU4W8CIGAIIuzjxBFlHpkqcsa7UZy24y/B6xZnktUw/Ne5q5hCAHcATnWjJ1yaEMM4W2zU3z9S6x3w4I4bjWnAsfpksWKaOd8AAAGOFjrv9wAABAMASDBGAiEA+8OvQzpgRf31uLBsCE8ktCUfvsiRT7zWSqeXliA09TUCIQDcB7Xn97aEDMBKXIbdm5KZ9GjvRyoF9skD5/4GneoMWzAlBgNVHREEHjAcggpnaXRodWIuY29tgg53d3cuZ2l0aHViLmNvbTAKBggqhkjOPQQDAgNIADBFAiEAru2McPr0eNwcWNuDEY0a/rGzXRfRrm+6XfZeSzhYZewCIBq4TUEBCgapv7xvAtRKdVdi/b4m36Uyej1ggyJsiesA");

using var ms = new MemoryStream(data);
var tag = Asn1Serializer.Deserialize(ms);

if (Asn1Serializer.TryDeserialize(data, out var tag2, out var error) is false)
{
    Console.WriteLine($"Data deserialization error: {error}");
    return;
}

if (tag.TryGetCertificateSubjectItem(SubjectItemKind.CommonName, true, out List<string> issuerCn))
{
    Console.WriteLine($"Issuer CN: {issuerCn[0]}");
}

if (tag.TryGetCertificateSubjectItem(SubjectItemKind.CommonName, false, out List<string> subjectCn))
{
    Console.WriteLine($"Subject CN: {subjectCn[0]}");
}

if(tag.TryGetCertificateNotAfter(out var notAfter))
{
    Console.WriteLine($"Not After: {notAfter}");
}

if (tag.TryGetKeyUsage(out var ku))
{
    Console.WriteLine($"Key Usage: {ku}");
}
```

Output:

```plaintext
Issuer CN: Sectigo ECC Domain Validation Secure Server CA
Subject CN: github.com
Not After: 07.03.2025 23:59:59
Key Usage: DigitalSignature, NonRepudiation
```

### Serialize ASN.1 Data

This library also provides functionality for serializing ASN.1 data. The following example demonstrates how to serialize an ASN.1 tag.

```cs
using System;
using System.Text;
using fASN1.NET;
using fASN1.NET.Tags;

var root = new Sequence([
    new Integer([1]),
    new Sequence([
        new ObjectIdentifier([0,1,35,45,55,127,126]),
        new Utf8String(Encoding.UTF8.GetBytes("Random text")),
        new ContextSpecific_0(children: [
            new BitString(content: [123,125,222,255,0,1,4])
            ])
    ])
]);

var serialized = Asn1Serializer.Serialize(root);
var deserializedTag = Asn1Serializer.Deserialize(serialized);
var deserializedText = Asn1Serializer.TagToString(deserializedTag);
Console.WriteLine(deserializedText);
```

The data looks like this after deserialization into a string:

```asn1
Sequence
 | Integer 1
 | Sequence
 |  | ObjectIdentifier 0.0.1.35.45.55.127.126
 |  | UTF8String Random text
 |  | [0]
 |  |  | BitString 011110110111110111011110111111110000000000000001000
```

## Performance


The following table shows the performance of the current implementation when working with certificates.

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
AMD Ryzen 7 7840HS with Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.300
  [Host]     : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
````

| Method                          | Mean      | Error     | StdDev    | Gen0    | Gen1   | Allocated |
|-------------------------------- |----------:|----------:|----------:|--------:|-------:|----------:|
| DeserializeCertificate          |  4.846 us | 0.0961 us | 0.1733 us |  2.0294 | 0.1068 |  16.61 KB |
| SerializeCertificateTagToString | 50.612 us | 0.3710 us | 0.3470 us | 10.9863 | 0.4883 |  90.45 KB |

The previous version of the library ([Asn1DecoderNet5](https://github.com/AVVI94/ASN1Decoder)) was like this:

| Method                          | Mean      | Error     | StdDev    | Gen0    | Gen1   | Allocated |
|-------------------------------- |----------:|----------:|----------:|--------:|-------:|----------:|
| DeserializeCertificate          |  27.93 us |  0.105 us |  0.088 us |  2.7161 | 0.1526 |  22.23 KB |
| SerializeCertificateTagToString |  89.47 us |  1.723 us |  1.527 us | 46.6309 | 7.2021 | 381.05 KB |

The performance has been significantly improved, especially when serializing the data to a string.

Note: The smaller and less complex the data, the better the performance (especially the memory allocation).