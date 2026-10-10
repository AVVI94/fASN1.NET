# Changelog

## 2.0.0

A rewrite of the parser and a review of the whole library against X.690, RFC 5280 and RFC 2986. Several fixes
change results that callers may depend on, hence the major version. Version 1 and 2 certificates remain
unsupported by design: only version 3 can carry extensions.

### Security

- **Exponential parse time.** A few hundred bytes of nested OCTET STRINGs could keep the parser busy for years.
  Every element is now bounded by its innermost definite-length container, so parsing is linear.
- **Stack overflows.** The parser, `Serialize`, `TagToString`, `TryGetKeyUsage` and the subject item search were
  recursive; about 5 KB of nested input terminated the process with an uncatchable stack overflow. All of them are
  iterative now and handle any depth that fits in memory.
- **Large allocations from corrupted lengths.** A declared length is checked against the input before its buffer
  is allocated; lengths above `int.MaxValue` are rejected instead of wrapping around.
- **OID registry.** `OID.GetOrCreate` wrote to a shared `Dictionary` from every thread and kept every OID ever seen
  in input. The registry is now thread-safe and unknown OIDs are not added to it.

### Breaking changes

Parsing:
- An OCTET STRING is read as encapsulated children only when every length inside it is in DER form and every SET
  inside it is in DER order; otherwise it stays a leaf with its raw `Content`. This guarantees that `Serialize` writes its value back unchanged.
- Input that used to parse into a wrong tree now fails with an error: multi-byte tag numbers, an element crossing
  the end of its container, a missing end-of-contents marker, `00` with a non-zero length inside an
  indefinite-length container, lengths with more than 6 octets or above `int.MaxValue`.
- `TryDeserialize` always gives an error message when it returns `false`. An exception from a custom tag factory or
  from the stream propagates out of `Deserialize`.
- `ITag.Children` of a parsed tag is an `IList<ITag>` that is not a `List<ITag>`; it is still fully mutable.
- Every built-in tag derives from the new public `TagBase`. `TagNumber` of `Application`, `Private` and `Unknown`
  tags is the identifier byte (it was 0). `EmbeddedPdv.TagNumber` and `Tag.EmbeddedPDV` are `0x2B` (they were 10).

Writing:
- `Serialize` throws `ArgumentException` for a tag number that does not fit one identifier byte and for an element
  that has both content and children (the content used to be dropped). An element with children gets the
  constructed bit, except a universal BIT STRING or OCTET STRING; a BIT STRING with children gets its leading
  unused-bits byte. The children of every SET are written in DER order (X.690 §11.6).
- `Integer(int/long/…/BigInteger)` writes DER two's complement (128 is `00 80`; it was `80`, which reads as -128).
- `UTCTime(DateTime)` and `GeneralizedTime(DateTime)` write DER: UTC with `Z`, invariant culture, and for
  GeneralizedTime a fraction of a second only when there is one. `UTCTime` throws for years outside 1950–2049.
- `BitString(byte[])` derives the unused-bits count from the trailing zero bits; it used to be reversed.
  `DefaultTagFactory.Create(3, content)` keeps the content as given instead of adding a second unused-bits byte.
- `OidEncoder.GetString` and `GetBytes` throw `ArgumentException` for invalid input instead of returning a wrong OID.
  Text arcs must be decimal numbers without sign or leading zeros. New: `TryGetString`, `TryGetBytes`.

Text output (`TagToString`, `ContentToString`, the parsing strategies):
- Times are shown as invariant ISO 8601, such as `2025-01-02T03:04:05Z`, instead of in the current culture.
- OCTET STRING and non-universal content is shown as text only when it is valid UTF-8 without control characters,
  otherwise as hexadecimal.
- BOOLEAN shows any non-zero byte as `True`. UniversalString is decoded big-endian. TeletexString is decoded as
  UTF-8 or Latin-1 instead of Base64 (new `TeletexStringParsingStrategy`). INTEGER text is culture-invariant.
- A malformed value or a failing custom strategy is shown as hexadecimal instead of throwing.

Certificate helpers:
- `IsCertificate` checks every tbsCertificate field up to subjectPublicKeyInfo; `IsCertificateRequest` checks the
  certificationRequestInfo of RFC 2986 (a missing attributes field is tolerated), so a version 1 certificate with
  serial number 0 is no longer taken for a request.
- `TryGetSubjectAlternativeName` returns `false` when there is no SAN extension (it returned `true`).
- `TryGetSubjectKeyIdentifier` and `TryGetAuthorityKeyIdentifier` always return uppercase hexadecimal of the key
  identifier bytes. `TryGetAuthorityKeyIdentifier` returns `false` when the extension has no keyIdentifier.
- `TryGetCertificateNotBefore` and `TryGetCertificateNotAfter` return `DateTimeKind.Utc`.
- SAN: an `IPAddress` is formatted from its bytes (new `IPAddress.Bytes`); `X400Address.Content` is the `[3]`
  element itself; registeredID and ediPartyName are read with their RFC 5280 tagging.
- `TryGetKeyUsage` follows RFC 5280 bit order; the `KeyUsage` constructors no longer set `DecipherOnly` when no
  other flag is set, and a new overload takes it explicitly.
- `OID.OidDictionary` is an `IReadOnlyDictionary`; add names with the new `OID.Register`. `OID.GetOrCreate` returns
  a new instance for an unknown OID instead of caching it.
- The `TryGet` methods return `false` for malformed input instead of throwing.

### Other fixes

- Critical extensions (SAN, EKU, subject directory attributes, I.CA interconnection) were not found, because the
  critical flag was read as the extension value.
- Multi-valued RDNs (`CN=a+serialNumber=b`) are searched completely, and the country attributes of subject
  directory attributes return every value.
- OIDs with a large first subidentifier (2.999) or arcs above 2^31 (UUID OIDs) are decoded correctly.
- Times with offsets, without seconds or with fractions are read correctly.
- Short `Stream.Read` results no longer leave zeros in content.
- An empty indefinite-length container no longer swallows the bytes after it, and indefinite-length containers no
  longer drop children on errors or on tags mapped to `Unknown`.
- `Serialize` wrote wrong lengths for some sizes from 64 KiB up.

### Performance

- Parsing allocates only the returned tree: 6.83 KB for the GitHub certificate in the README (16.3 KB before), and
  a tag object takes 24 bytes. No buffers are kept between calls or per thread.
