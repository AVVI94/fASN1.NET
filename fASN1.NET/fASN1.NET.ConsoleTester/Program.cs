// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using fASN1.NET;
using fASN1.NET.Tags;

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
    Console.WriteLine($"Issuer CN: {subjectCn[0]}");
}

if (tag.TryGetCertificateNotAfter(out var notAfter))
{
    Console.WriteLine($"Not After: {notAfter}");
}

if (tag.TryGetKeyUsage(out var ku))
{
    Console.WriteLine($"Key Usage: {ku}");
}

//var root = new Sequence([
//    new Integer([1]),
//    new Sequence([
//        new ObjectIdentifier([0,1,35,45,55,127,126]),
//        new Utf8String(Encoding.UTF8.GetBytes("Random text")),
//        new ContextSpecific_0(children: [
//            new BitString(content: [123,125,222,255,0,1,4])
//            ])
//    ])
//]);

//var serialized = Asn1Serializer.Serialize(root);
//var deserializedTag = Asn1Serializer.Deserialize(serialized);
//var deserializedText = Asn1Serializer.TagToString(deserializedTag);
//Console.WriteLine(deserializedText);