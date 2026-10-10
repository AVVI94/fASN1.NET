using System;
using System.Collections.Generic;
using System.Linq;
using fASN1.NET.Oid;
using fASN1.NET.Tags;

namespace fASN1.NET;

/// <summary>
/// Navigation of X.509 certificates (RFC 5280) and PKCS#10 certification requests (RFC 2986) in a parsed tree.
/// Every method tolerates malformed trees: a missing or unexpected element gives <see langword="null"/>, an empty
/// list or <see langword="false"/>, never an exception.
/// </summary>
internal static class InternalExtensions
{
    public static readonly byte[] _version0Sequence = [0x00];
    public static readonly byte[] _version2Sequence = [0x02];
    public static readonly byte[] _booleanTrueSequence = [0xFF];
    public static readonly byte[] _keyUsageOidSequence = OID.GetOrCreate(OID.KEY_USAGE).ByteValue;
    public static readonly byte[] _sanOidSequence = OID.GetOrCreate(OID.SUBJECT_ALT_NAME).ByteValue;
    public static readonly byte[] _icaUserIdSequence = OID.GetOrCreate(OID.ICA_USER_ID).ByteValue;
    public static readonly byte[] _icaIkMpsvSequence = OID.GetOrCreate(OID.ICA_IK_MPSV).ByteValue;
    public static readonly byte[] _icaCertInterconnectionSequence = OID.GetOrCreate(OID.ICA_CERT_INTERCONNECTION).ByteValue;
    public static readonly byte[] _extensionRequestSequence = OID.GetOrCreate(OID.EXTENSION_REQUEST).ByteValue;
    public static readonly byte[] _extKeyUsageSequence = OID.GetOrCreate(OID.EXT_KEY_USAGE).ByteValue;
    public static readonly byte[] _authorityKeyIdentifierOidSequence = OID.GetOrCreate(OID.AUTHORITY_KEY_IDENTIFIER).ByteValue;
    public static readonly byte[] _subjectKeyIdentifierOidSequence = OID.GetOrCreate(OID.SUBJECT_KEY_IDENTIFIER).ByteValue;
    public static readonly byte[] _subjectDirectoryAttributesOidSequence = OID.GetOrCreate(OID.SUBJECT_DIRECTORY_ATTRIBUTES).ByteValue;

    public static readonly byte[] _countryOfCitizenshipOidSequence = OID.GetOrCreate(OID.COUNTRY_OF_CITIZENSHIP).ByteValue;
    public static readonly byte[] _countryOfResidenceOidSequence = OID.GetOrCreate(OID.COUNTRY_OF_RESIDENCE).ByteValue;
    public static readonly byte[] _genderOidSequence = OID.GetOrCreate(OID.GENDER).ByteValue;
    public static readonly byte[] _dateOfBirthOidSequence = OID.GetOrCreate(OID.DATE_OF_BIRTH).ByteValue;
    public static readonly byte[] _placeOfBirthOidSequence = OID.GetOrCreate(OID.PLACE_OF_BIRTH).ByteValue;

    /// <summary>Identifier byte of a constructed context-specific tag [<paramref name="number"/>].</summary>
    private static int ContextConstructed(int number) => 0xA0 | number;

    #region Structure
    /// <summary>
    /// Whether <paramref name="tag"/> is an X.509 v3 certificate (RFC 5280 §4.1):
    /// <c>SEQUENCE { tbsCertificate, signatureAlgorithm AlgorithmIdentifier, signatureValue BIT STRING }</c> with
    /// <c>tbsCertificate ::= SEQUENCE { version [0] EXPLICIT INTEGER (2), serialNumber INTEGER, signature
    /// AlgorithmIdentifier, issuer Name, validity SEQUENCE { notBefore, notAfter }, subject Name,
    /// subjectPublicKeyInfo, ... }</c>.
    /// </summary>
    /// <remarks>
    /// Version 1 and 2 certificates are not supported by design; only version 3 can carry extensions.
    /// </remarks>
    public static bool IsV3Certificate(ITag tag)
    {
        if (!IsSignedSequence(tag) || tag.Children[0] is not { TagNumber: (int)Tag.Sequence } tbs || tbs.Children.Count < 7)
            return false;

        var version = tbs.Children[0];
        return version.TagNumber == ContextConstructed(0)
               && version.Children.Count == 1
               && version.Children[0] is { TagNumber: (int)Tag.Integer } versionNumber
               && versionNumber.Content.SequenceEqual(_version2Sequence)
               && tbs.Children[1].TagNumber == (int)Tag.Integer
               && IsAlgorithmIdentifier(tbs.Children[2])
               && IsName(tbs.Children[3])
               && tbs.Children[4] is { TagNumber: (int)Tag.Sequence, Children.Count: 2 }
               && IsName(tbs.Children[5])
               && IsSubjectPublicKeyInfo(tbs.Children[6]);
    }

    /// <summary>
    /// Whether <paramref name="tag"/> is a PKCS#10 certification request (RFC 2986 §4):
    /// <c>SEQUENCE { certificationRequestInfo, signatureAlgorithm AlgorithmIdentifier, signature BIT STRING }</c>
    /// with <c>certificationRequestInfo ::= SEQUENCE { version INTEGER (0), subject Name, subjectPKInfo,
    /// attributes [0] IMPLICIT SET OF Attribute }</c>.
    /// </summary>
    /// <remarks>
    /// The attributes field is required, even when empty. Checking every field keeps a version 1 certificate with
    /// serial number 0, whose first fields look the same, from being taken for a request.
    /// </remarks>
    public static bool IsCertificationRequest(ITag tag)
    {
        if (!IsSignedSequence(tag) || tag.Children[0] is not { TagNumber: (int)Tag.Sequence, Children.Count: 4 } info)
            return false;

        return info.Children[0] is { TagNumber: (int)Tag.Integer } version
               && version.Content.SequenceEqual(_version0Sequence)
               && IsName(info.Children[1])
               && IsSubjectPublicKeyInfo(info.Children[2])
               && info.Children[3].TagNumber == ContextConstructed(0);
    }

    private static bool IsSignedSequence(ITag tag)
        => tag.TagNumber == (int)Tag.Sequence
           && tag.Children.Count == 3
           && IsAlgorithmIdentifier(tag.Children[1])
           && tag.Children[2] is { TagNumber: (int)Tag.BitString, Children.Count: 0 };

    /// <summary><c>AlgorithmIdentifier ::= SEQUENCE { algorithm OBJECT IDENTIFIER, parameters ANY OPTIONAL }</c></summary>
    private static bool IsAlgorithmIdentifier(ITag tag)
        => tag is { TagNumber: (int)Tag.Sequence, Children.Count: 1 or 2 }
           && tag.Children[0].TagNumber == (int)Tag.ObjectIdentifier;

    /// <summary><c>Name ::= SEQUENCE OF RelativeDistinguishedName</c>, each a SET.</summary>
    private static bool IsName(ITag tag)
        => tag.TagNumber == (int)Tag.Sequence && tag.Children.All(IsSET);

    /// <summary><c>SubjectPublicKeyInfo ::= SEQUENCE { algorithm AlgorithmIdentifier, subjectPublicKey BIT STRING }</c></summary>
    private static bool IsSubjectPublicKeyInfo(ITag tag)
        => tag is { TagNumber: (int)Tag.Sequence, Children.Count: 2 }
           && IsAlgorithmIdentifier(tag.Children[0])
           && tag.Children[1].TagNumber == (int)Tag.BitString;

    /// <summary>The tbsCertificate fields of a certificate that passed <see cref="IsV3Certificate"/>.</summary>
    public static IList<ITag> TbsFields(ITag certificate) => certificate.Children[0].Children;

    /// <summary>The subject Name of a request that passed <see cref="IsCertificationRequest"/>.</summary>
    public static ITag RequestSubject(ITag request) => request.Children[0].Children[1];
    #endregion

    #region Extensions
    /// <summary>
    /// The extensions of a v3 certificate (<c>extensions [3] EXPLICIT Extensions</c> in tbsCertificate), or of a
    /// request (the extensionRequest attribute, PKCS#9); empty when there are none or the tree is neither.
    /// </summary>
    /// <param name="root">The certificate or request.</param>
    /// <param name="certificatesOnly">Ignore requests.</param>
    public static IList<ITag> GetExtensions(ITag root, bool certificatesOnly = false)
    {
        if (IsV3Certificate(root))
        {
            foreach (var field in TbsFields(root).Skip(7))
            {
                if (field.TagNumber == ContextConstructed(3) && field.Children.Count == 1 && field.Children[0].TagNumber == (int)Tag.Sequence)
                    return field.Children[0].Children;
            }
            return [];
        }
        if (!certificatesOnly && IsCertificationRequest(root))
        {
            // Attribute ::= SEQUENCE { type OBJECT IDENTIFIER, values SET OF value }; extensionRequest has one value.
            foreach (var attribute in root.Children[0].Children[3].Children)
            {
                if (attribute is { TagNumber: (int)Tag.Sequence, Children.Count: 2 }
                    && IsOid(attribute.Children[0], _extensionRequestSequence)
                    && attribute.Children[1] is { TagNumber: (int)Tag.Set, Children.Count: >= 1 } values
                    && values.Children[0].TagNumber == (int)Tag.Sequence)
                {
                    return values.Children[0].Children;
                }
            }
        }
        return [];
    }

    /// <summary>
    /// The first extension with the given OID, checked to be
    /// <c>SEQUENCE { extnID OBJECT IDENTIFIER, critical BOOLEAN DEFAULT FALSE, extnValue OCTET STRING }</c>.
    /// </summary>
    public static ITag? FindExtension(ITag root, byte[] oid, bool certificatesOnly = false)
    {
        foreach (var extension in GetExtensions(root, certificatesOnly))
        {
            if (IsExtension(extension) && IsOid(extension.Children[0], oid))
                return extension;
        }
        return null;
    }

    /// <summary>Whether <paramref name="tag"/> has the shape of an Extension.</summary>
    public static bool IsExtension(ITag tag)
        => tag is { TagNumber: (int)Tag.Sequence, Children.Count: 2 or 3 }
           && tag.Children[0].TagNumber == (int)Tag.ObjectIdentifier
           && (tag.Children.Count == 2 || tag.Children[1].TagNumber == (int)Tag.Boolean)
           && GetExtensionValue(tag).TagNumber == (int)Tag.OctetString;

    /// <summary>
    /// The extnValue OCTET STRING of an extension: <c>SEQUENCE { extnID, critical BOOLEAN DEFAULT FALSE, extnValue }</c>,
    /// so it is the last child whether or not the critical flag is present.
    /// </summary>
    public static ITag GetExtensionValue(ITag extension) => extension.Children[extension.Children.Count - 1];

    /// <summary>Whether the extension is marked critical.</summary>
    public static bool IsCritical(ITag extension)
        => extension.Children.Count == 3 && extension.Children[1].Content.SequenceEqual(_booleanTrueSequence);

    /// <summary>
    /// The element encoded in the extnValue of <paramref name="extension"/>. The parser reads extnValue as that
    /// element when it is in DER form; a value in another BER form stays raw bytes and is decoded here.
    /// </summary>
    public static ITag? GetExtensionContent(ITag extension)
    {
        var value = GetExtensionValue(extension);
        if (value.Children.Count == 1)
            return value.Children[0];
        if (value.Children.Count == 0 && value.Content.Length > 0
            && Asn1Serializer.TryDeserialize(value.Content, out var decoded, out _))
        {
            return decoded;
        }
        return null;
    }

    /// <summary>The decoded value of the first extension with the given OID.</summary>
    public static ITag? FindExtensionContent(ITag root, byte[] oid, bool certificatesOnly = false)
        => FindExtension(root, oid, certificatesOnly) is { } extension ? GetExtensionContent(extension) : null;
    #endregion

    #region Key usage
    public static bool IsKeyUsageSequence(this ITag tag)
        => IsExtension(tag) && IsOid(tag.Children[0], _keyUsageOidSequence);

    public static bool IsKeyUsageCritical(this ITag tag)
         => tag.IsKeyUsageSequence() && IsCritical(tag);

    /// <summary>The KeyUsage BIT STRING of a key usage extension, or <see langword="null"/>.</summary>
    public static ITag? GetKeyUsageBitStringTag(this ITag tag)
        => GetExtensionContent(tag) is { TagNumber: (int)Tag.BitString, Children.Count: 0 } bits ? bits : null;
    #endregion

    #region Content
    /// <summary>
    /// The content bytes of a primitive element as they were in the input. An OCTET STRING read as encapsulated
    /// children has empty <see cref="ITag.Content"/>; its bytes are those children serialized, which the parser
    /// guarantees is the original encoding.
    /// </summary>
    public static byte[] GetRawContent(ITag tag)
    {
        if (tag.Children.Count == 0)
            return tag.Content;

        var bytes = new List<byte>();
        foreach (var child in tag.Children)
        {
            bytes.AddRange(Asn1Serializer.Serialize(child));
        }
        return bytes.ToArray();
    }

    public static bool IsOid(ITag tag, byte[] oid)
        => tag.TagNumber == (int)Tag.ObjectIdentifier && tag.Content.SequenceEqual(oid);

    public static bool IsSET(ITag t) => t.TagNumber == (int)Tag.Set;

    public static bool IsOidNullOrEmpty(string? oid)
    {
        return string.IsNullOrWhiteSpace(oid);
    }
    #endregion

    #region Names
    /// <summary>
    /// Finds every value of the attribute <paramref name="oid"/> in a Name, in document order.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Name ::= SEQUENCE OF RelativeDistinguishedName
    /// RelativeDistinguishedName ::= SET SIZE (1..MAX) OF AttributeTypeAndValue
    /// AttributeTypeAndValue ::= SEQUENCE { type OBJECT IDENTIFIER, value ANY }
    /// </code>
    /// Every AttributeTypeAndValue of a multi-valued RDN (such as <c>CN=a+OU=b</c>) is searched. SETs are matched,
    /// not entered; every other element is searched, so the method also works on trees that hold Names deeper down.
    /// </remarks>
    /// <param name="tag">The Name, or an element containing Names.</param>
    /// <param name="oid">Requested OID in byte array representation. <see cref="OidEncoder"/> can convert it from a string.</param>
    /// <param name="items">The values, as text.</param>
    /// <returns><see langword="true" /> if any value was found, otherwise <see langword="false" />.</returns>
    public static bool TryGetSubjectItem(this ITag tag, byte[] oid, out List<string> items)
    {
        items = new List<string>();
        if (oid is null or { Length: 0 } || tag.Children.Count == 0)
            return false;

        // Pre-order walk with an explicit stack, so values come in document order and deep trees cannot overflow
        // the call stack.
        var pending = new Stack<ITag>();
        if (IsSET(tag))
        {
            AddMatchingValues(tag, oid, items);
        }
        PushChildren(pending, tag);
        while (pending.Count > 0)
        {
            var t = pending.Pop();
            if (IsSET(t))
            {
                AddMatchingValues(t, oid, items);
            }
            else
            {
                PushChildren(pending, t);
            }
        }
        return items.Count > 0;

        static void PushChildren(Stack<ITag> pending, ITag t)
        {
            for (int i = t.Children.Count - 1; i >= 0; i--)
            {
                pending.Push(t.Children[i]);
            }
        }
    }

    /// <summary>Adds the value of every AttributeTypeAndValue of the RDN <paramref name="set"/> whose type is <paramref name="oid"/>.</summary>
    private static void AddMatchingValues(ITag set, byte[] oid, List<string> items)
    {
        foreach (var attribute in set.Children)
        {
            if (TryGetAttributeTypeAndValue(attribute, out var type, out var value) && IsOid(type, oid))
            {
                items.Add(value.ContentToString());
            }
        }
    }

    /// <summary>Splits <c>AttributeTypeAndValue ::= SEQUENCE { type OBJECT IDENTIFIER, value ANY }</c>.</summary>
    public static bool TryGetAttributeTypeAndValue(ITag attribute, out ITag type, out ITag value)
    {
        type = value = attribute;
        if (attribute is not { TagNumber: (int)Tag.Sequence, Children.Count: 2 }
            || attribute.Children[0].TagNumber != (int)Tag.ObjectIdentifier)
        {
            return false;
        }
        type = attribute.Children[0];
        value = attribute.Children[1];
        return true;
    }
    #endregion
}
