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
  in input. The registry is now thread-safe and unknown OIDs are not added to it. `OID.OidDictionary` is a
  read-only view of it; it used to be the registry itself, which a cast to `IDictionary` could modify.

### Breaking changes

Tag identity:
- `ITag.TagNumber` is the tag number within its class for every tag (16 for SEQUENCE, 0 for `[0]`, 1000 for
  `[1000]`); it used to be the identifier byte for some classes (SEQUENCE was 48, `[0]` was 160). `Tag.Sequence`,
  `Tag.Set` and `Tag.EmbeddedPDV` are 16, 17 and 11. As the number alone no longer identifies a universal type
  (`[16]` also has 16), use the new `tag.Is(Tag.Sequence)` instead of comparing `TagNumber` with a `Tag` value.
- Multi-byte tag numbers (31 and up, X.690 §8.1.2.4) are read and written; they used to be rejected.
- `ITagFactory.Create(int tagClass, bool isConstructed, int tagNumber, byte[]? content)` replaces
  `Create(int identifier, byte[]? content)`. The parser decides the shape of the tree from the encoding alone, as
  X.690 defines it: a tag whose `IsConstructed` disagrees with its identifier no longer changes how its content is
  read. The factory is asked only for the tags of the final tree, and for a tag's name when an error message needs it.
  Contents or children a tag from the factory already has are removed before the parser fills it; they used to be
  kept, and children took the place of the contents octets.
- `ContextSpecific_0` to `ContextSpecific_3` take `isConstructed` instead of an identifier byte;
  `ContextSpecificTag`, `Application` and `Private` take the tag number and `isConstructed`, and `Unknown` also the
  class. These tags report `IsConstructed` from their identifier, so a parsed empty `A0 00` is constructed.

Parsing:
- BER constructed strings (a BIT STRING, OCTET STRING, character string or time split into fragments, as CER does for
  values over 1000 bytes) are joined into one value: the tree has the same tag as for the primitive encoding, and a
  joined OCTET STRING is read for encapsulated elements like a primitive one. They used to be `Unknown` tags with
  the fragments as children and no content. `Serialize` writes them in the primitive DER form.
- An OCTET STRING is read as encapsulated children only when every length inside it is in DER form and every SET
  inside it is in DER order; otherwise it stays a leaf with its raw `Content`. This guarantees that `Serialize` writes its value back unchanged.
- Input that used to parse into a wrong tree now fails with an error: an element crossing
  the end of its container, a missing end-of-contents marker, `00` with a non-zero length inside an
  indefinite-length container, lengths with more than 6 octets or above `int.MaxValue`. End-of-contents octets
  anywhere but at the end of contents in the indefinite form (X.690 §8.1.5), such as `00 00` at the root or inside
  a definite-length SEQUENCE, are an error; they used to become an `Eoc` tag in the tree.
- `TryDeserialize` never throws, in every overload, and always gives an error message when it returns `false`: for
  invalid data, a `null` argument (the byte-array overloads used to throw `ArgumentNullException`), a stream that
  cannot seek, or an exception from the stream or a custom tag factory. `Deserialize` throws
  `Asn1DeserializationException` for invalid data, empty data included (it used to throw `ArgumentException`),
  `ArgumentNullException` for a `null` argument (a `null` stream used to give `ArgumentException`),
  `ArgumentException` for a stream that cannot seek, and lets an exception from the stream or a custom tag factory
  propagate. The message for empty data is "The data is empty" instead of "Stream is null or empty.".
- Error messages use the terms of X.690 (identifier, length and contents octets, encodings, end-of-contents octets,
  fragments), for example "The contents in the indefinite form at offset 2 have no end-of-contents octets" instead
  of "Indefinite-length container at offset 2 has no end-of-contents marker".
- `ITag.Children` of a parsed tag is an `IList<ITag>` that is not a `List<ITag>`; it is still fully mutable.
- Every built-in tag derives from the new public `TagBase`.

Writing:
- `Serialize` throws `ArgumentException` for a class outside 0–3 or a negative tag number, and for an element
  that has both content and children (the content used to be dropped). An element with children gets the
  constructed bit, except a universal BIT STRING or OCTET STRING; a BIT STRING with children gets its leading
  unused-bits byte. The children of every SET are written in DER order (X.690 §11.6).
- `Integer(int/long/…/BigInteger)` writes DER two's complement (128 is `00 80`; it was `80`, which reads as -128).
- `UTCTime(DateTime)` and `GeneralizedTime(DateTime)` write DER: UTC with `Z`, invariant culture, and for
  GeneralizedTime a fraction of a second only when there is one. `UTCTime` throws for years outside 1950–2049.
- `BitString(byte[])` derives the unused-bits count from the trailing zero bits; it used to be reversed.
  `DefaultTagFactory.Create(0, false, 3, content)` keeps the content as given instead of adding a second unused-bits byte.
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
