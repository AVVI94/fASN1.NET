using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using fASN1.NET.ContentParsing;
using fASN1.NET.Oid;
using fASN1.NET.Tags;
using fASN1.NET.Tags.San;
using static fASN1.NET.InternalExtensions;
using IPAddress = fASN1.NET.Tags.San.IPAddress;

namespace fASN1.NET;

/// <summary>
/// Reads common fields of X.509 certificates and PKCS#10 certificate requests from a parsed tree.
/// </summary>
/// <remarks>
/// Only version 3 certificates are supported, by design: versions 1 and 2 cannot carry extensions. The
/// <c>TryGet</c> methods never throw for malformed input; they return <see langword="false"/>.
/// </remarks>
public static partial class Extensions
{
    /// <summary>
    /// Determines whether the specified tag is a PKCS#10 certification request (RFC 2986).
    /// </summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns><see langword="true"/> if the tag represents a certificate request; otherwise, <see langword="false"/>.</returns>
    public static bool IsCertificateRequest(this ITag tag) => IsCertificationRequest(tag);

    /// <summary>
    /// Determines whether the specified tag is an X.509 version 3 certificate (RFC 5280).
    /// </summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns><see langword="true"/> if the tag represents a X.509v3 certificate; otherwise, <see langword="false"/>.</returns>
    public static bool IsCertificate(this ITag tag) => IsV3Certificate(tag);

    /// <summary>
    /// Attempts to get the extended key usage from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="certificateTag">The certificate tag to check.</param>
    /// <param name="eku">The extended key usage object.</param>
    /// <returns><see langword="true"/> if the extended key usage was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetExtendedKeyUsage(this ITag certificateTag, out ExtendedKeyUsage eku)
    {
        eku = new ExtendedKeyUsage();
        try
        {
            // ExtKeyUsageSyntax ::= SEQUENCE SIZE (1..MAX) OF KeyPurposeId (OBJECT IDENTIFIER)
            if (FindExtensionContent(certificateTag, _extKeyUsageSequence, certificatesOnly: true) is not { TagNumber: (int)Tag.Sequence } purposes)
                return false;

            bool ekuFound = false;
            foreach (var e in purposes.Children)
            {
                if (e.TagNumber != (int)Tag.ObjectIdentifier)
                    continue;
                ekuFound = true;
                switch (OidEncoder.GetString(e.Content))
                {
                    case OID.EKU_EMAIL_PROTECTION:
                        eku.EmailProtection = true;
                        break;
                    case OID.EKU_CLIENT_AUTH:
                        eku.ClientAuth = true;
                        break;
                    case OID.EKU_SERVER_AUTH:
                        eku.ServerAuth = true;
                        break;
                    case OID.EKU_TIME_STAMPING:
                        eku.TimeStamping = true;
                        break;
                    case OID.EKU_CODE_SIGNING:
                        eku.CodeSigning = true;
                        break;
                    case OID.EKU_OCSP_SIGNING:
                        eku.OcspSigning = true;
                        break;
                    default:
                        eku.OtherEKUs ??= new List<OID>();
                        ((List<OID>)eku.OtherEKUs).Add(OID.GetOrCreate(e.Content));
                        break;
                }
            }
            return ekuFound;
        }
        catch
        {
            eku = new ExtendedKeyUsage();
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the I.CA certificate interconnection extension (OID 1.3.6.1.4.1.23624.4.7) from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="certificate">The certificate tag to check.</param>
    /// <param name="intercon">The interconnection data.</param>
    /// <returns><see langword="true"/> if the extension was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetICACertIntercon(this ITag certificate, out ICACertIntercon intercon)
    {
        intercon = default;
        try
        {
            // SEQUENCE { masterRequestId, interconnectedCertificatesCount INTEGER, isMaster BOOLEAN }
            if (FindExtensionContent(certificate, _icaCertInterconnectionSequence, certificatesOnly: true) is not { TagNumber: (int)Tag.Sequence, Children.Count: 3 } value
                || value.Children[1].TagNumber != (int)Tag.Integer
                || value.Children[2].TagNumber != (int)Tag.Boolean
                || !int.TryParse(value.Children[1].ContentToString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var count))
            {
                return false;
            }
            intercon = new ICACertIntercon(IsTrue(value.Children[2]),
                                           value.Children[0].ContentToString(),
                                           count);
            return true;
        }
        catch
        {
            intercon = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the key usage from the specified X.509v3 certificate or certificate request tag. For any
    /// other tree, the first key usage extension found anywhere in it is used.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="keyUsage">The key usage object.</param>
    /// <returns><see langword="true"/> if the key usage was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetKeyUsage(this ITag tag, out KeyUsage keyUsage)
    {
        keyUsage = default;
        try
        {
            if (IsV3Certificate(tag) || IsCertificationRequest(tag))
            {
                return FindExtension(tag, _keyUsageOidSequence) is { } extension
                       && TryReadKeyUsage(extension, out keyUsage);
            }
            return FindKeyUsage(tag, ref keyUsage);
        }
        catch
        {
            keyUsage = default;
            return false;
        }

        // Depth-first, pre-order: the first key usage extension in document order wins. An explicit stack keeps
        // deep trees from overflowing the call stack.
        static bool FindKeyUsage(ITag root, ref KeyUsage keyUsage)
        {
            var pending = new Stack<ITag>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var tag = pending.Pop();
                if (tag.IsKeyUsageSequence() && TryReadKeyUsage(tag, out keyUsage))
                    return true;

                for (int i = tag.Children.Count - 1; i >= 0; i--)
                {
                    pending.Push(tag.Children[i]);
                }
            }
            return false;
        }

        static bool TryReadKeyUsage(ITag extension, out KeyUsage keyUsage)
        {
            keyUsage = default;
            if (extension.GetKeyUsageBitStringTag() is not { } bits)
                return false;
            keyUsage = KeyUsage.FromBitStringContent(bits.Content, extension.IsKeyUsageCritical());
            return true;
        }
    }

    /// <summary>
    /// Attempts to get the serial number from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="serialNumber">The serial number of the certificate.</param>
    /// <returns><see langword="true"/> if the serial number was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateSerialNumber(this ITag tag, out string? serialNumber)
    {
        serialNumber = null;
        try
        {
            if (!IsV3Certificate(tag))
                return false;
            serialNumber = TbsFields(tag)[1].ContentToString();
            return true;
        }
        catch
        {
            serialNumber = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the subject alternative name (SAN) from the specified X.509v3 certificate or certificate request tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="san">The list of subject alternative name items.</param>
    /// <returns>
    /// <see langword="true"/> if the subject alternative name extension was found; otherwise (also when the
    /// certificate or request has none), <see langword="false"/>.
    /// </returns>
    public static bool TryGetSubjectAlternativeName(this ITag tag, out List<ISanItem> san)
    {
        san = new();
        try
        {
            // GeneralNames ::= SEQUENCE SIZE (1..MAX) OF GeneralName
            if (FindExtensionContent(tag, _sanOidSequence) is not { TagNumber: (int)Tag.Sequence } names)
                return false;
            foreach (var name in names.Children)
            {
                if (ReadGeneralName(name) is { } item)
                    san.Add(item);
            }
            return true;
        }
        catch
        {
            san = new();
            return false;
        }

        // GeneralName ::= CHOICE { otherName [0], rfc822Name [1] IA5String, dNSName [2] IA5String,
        //     x400Address [3] ORAddress, directoryName [4] Name, ediPartyName [5] EDIPartyName,
        //     uniformResourceIdentifier [6] IA5String, iPAddress [7] OCTET STRING, registeredID [8] OBJECT IDENTIFIER }
        // RFC 5280 uses implicit tagging, except that a tag on a CHOICE (Name, DirectoryString) is always explicit.
        static ISanItem? ReadGeneralName(ITag name)
        {
            if (name.TagClass != 2)
                return null;
            switch (name.TagNumber & 0x1F)
            {
                case 0:
                    // OtherName ::= SEQUENCE { type-id OBJECT IDENTIFIER, value [0] EXPLICIT ANY }
                    if (name.Children.Count != 2 || name.Children[0].TagNumber != (int)Tag.ObjectIdentifier || name.Children[1].Children.Count != 1)
                        return null;
                    if (name.Children[0].Content.SequenceEqual(_icaUserIdSequence))
                        return new IcaUserId(name.Children[1]);
                    if (name.Children[0].Content.SequenceEqual(_icaIkMpsvSequence))
                        return new IcaIkMpsv(name.Children[1]);
                    return new OtherName(OID.GetOrCreate(name.Children[0].Content), name.Children[1].Children[0]);
                case 1:
                    return new Rfc822Name(IsoStringParsingStrategy.Default.Parse(name.Content));
                case 2:
                    return new DnsName(IsoStringParsingStrategy.Default.Parse(name.Content));
                case 3:
                    // The [3] element is the ORAddress SEQUENCE itself.
                    return new X400Address(name);
                case 4:
                    return name.Children.Count == 1 ? new DirectoryName(name.Children[0]) : null;
                case 5:
                    // The [5] element is the EDIPartyName SEQUENCE itself.
                    return new EdiPartyName(name);
                case 6:
                    return new UniformResourceIdentifier(IsoStringParsingStrategy.Default.Parse(name.Content));
                case 7:
                    return new IPAddress(name.Content);
                case 8:
                    // The [8] element is the OBJECT IDENTIFIER itself.
                    return name.Content.Length > 0 ? new RegisteredID(OID.GetOrCreate(name.Content)) : null;
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Attempts to get the notAfter date from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="notAfter">The notAfter date of the certificate, in UTC (<see cref="DateTimeKind.Utc"/>).</param>
    /// <returns><see langword="true"/> if the notAfter date was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateNotAfter(this ITag tag, out DateTime notAfter)
    {
        notAfter = default;
        try
        {
            if (!IsV3Certificate(tag))
                return false;
            // Validity ::= SEQUENCE { notBefore Time, notAfter Time }, Time ::= CHOICE { UTCTime, GeneralizedTime }
            if (!Asn1Time.TryParse(TbsFields(tag)[4].Children[1], out notAfter))
                return false;
            return true;
        }
        catch
        {
            notAfter = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the notBefore date from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="notBefore">The notBefore date of the certificate, in UTC (<see cref="DateTimeKind.Utc"/>).</param>
    /// <returns><see langword="true"/> if the notBefore date was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateNotBefore(this ITag tag, out DateTime notBefore)
    {
        notBefore = default;
        try
        {
            if (!IsV3Certificate(tag))
                return false;
            if (!Asn1Time.TryParse(TbsFields(tag)[4].Children[0], out notBefore))
                return false;
            return true;
        }
        catch
        {
            notBefore = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the X.509 Certificate v3 extensions.
    /// </summary>
    /// <param name="tag">The certificate asn1 data.</param>
    /// <param name="extensions">The extensions <see cref="ITag"/> collection.</param>
    /// <returns>
    /// <see langword="true"/> if the extensions were found; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGetCertificateExtensions(this ITag tag, out IList<ITag> extensions)
    {
        extensions = [];
        try
        {
            extensions = GetExtensions(tag, certificatesOnly: true);
            return extensions.Count > 0;
        }
        catch
        {
            extensions = [];
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the Authority Key Identifier from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="authorityKeyIdentifier">The key identifier as uppercase hexadecimal.</param>
    /// <returns>
    /// <see langword="true"/> if the certificate has an Authority Key Identifier extension with a keyIdentifier;
    /// otherwise (also when the extension names the issuer only by name and serial number), <see langword="false"/>.
    /// </returns>
    public static bool TryGetAuthorityKeyIdentifier(this ITag tag, out string? authorityKeyIdentifier)
    {
        authorityKeyIdentifier = null;
        try
        {
            // AuthorityKeyIdentifier ::= SEQUENCE { keyIdentifier [0] IMPLICIT OCTET STRING OPTIONAL, ... }
            if (FindExtensionContent(tag, _authorityKeyIdentifierOidSequence, certificatesOnly: true) is not { TagNumber: (int)Tag.Sequence } aki)
                return false;
            foreach (var field in aki.Children)
            {
                if (field.TagClass == 2 && (field.TagNumber & 0x1F) == 0)
                {
                    authorityKeyIdentifier = OctetStringParsingStrategy.ToHex(GetRawContent(field));
                    return true;
                }
            }
            return false;
        }
        catch
        {
            authorityKeyIdentifier = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the Subject Key Identifier from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="subjectKeyIdentifier">The key identifier as uppercase hexadecimal.</param>
    /// <returns><see langword="true"/> if the Subject Key Identifier was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetSubjectKeyIdentifier(this ITag tag, out string? subjectKeyIdentifier)
    {
        subjectKeyIdentifier = null;
        try
        {
            // SubjectKeyIdentifier ::= KeyIdentifier ::= OCTET STRING
            if (FindExtensionContent(tag, _subjectKeyIdentifierOidSequence, certificatesOnly: true) is not { TagNumber: (int)Tag.OctetString } keyId)
                return false;
            subjectKeyIdentifier = OctetStringParsingStrategy.ToHex(GetRawContent(keyId));
            return true;
        }
        catch
        {
            subjectKeyIdentifier = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the raw unprocessed Subject Directory Attributes from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">
    /// The certificate tag to check.
    /// </param>
    /// <returns>
    /// The Subject Directory Attributes <see cref="ITag"/> object.
    /// </returns>
    public static ITag? GetSubjectDirectoryAttributesRaw(this ITag tag)
    {
        try
        {
            return FindExtensionContent(tag, _subjectDirectoryAttributesOidSequence, certificatesOnly: true);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to get the Subject Directory Attributes from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="attrs">The Subject Directory Attributes object, or <see langword="null"/> if the Subject Directory Attributes were not found.</param>
    /// <returns><see langword="true"/> if the Subject Directory Attributes were found and parsed; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetSubjectDirectoryAttributesFromCertificate(this ITag tag, [NotNullWhen(true)] out SubjectDirectory.SubjectDirectoryAttributes? attrs)
    {
        attrs = null;
        try
        {
            if (GetSubjectDirectoryAttributesRaw(tag) is not ITag sda)
                return false;
            attrs = SubjectDirectory.SubjectDirectoryAttributes.FromTag(sda);
            return attrs is not null;
        }
        catch
        {
            attrs = null;
            return false;
        }
    }
}

/// <summary>
/// Attributes of a distinguished name (the subject or issuer of a certificate); see <see cref="Extensions.ToOidString"/> for their OIDs.
/// </summary>
public enum SubjectItemKind
{
    /// <summary>commonName (CN), 2.5.4.3.</summary>
    CommonName,
    /// <summary>givenName, 2.5.4.42.</summary>
    GivenName,
    /// <summary>surname (SN), 2.5.4.4.</summary>
    Surname,
    /// <summary>countryName (C), 2.5.4.6.</summary>
    CountryName,
    /// <summary>organizationName (O), 2.5.4.10.</summary>
    OrganizationName,
    /// <summary>organizationalUnitName (OU), 2.5.4.11.</summary>
    OrganizationUnitName,
    /// <summary>organizationIdentifier, 2.5.4.97.</summary>
    OrganizationIdentifier,
    /// <summary>stateOrProvinceName (ST), 2.5.4.8.</summary>
    StateOrProvinceName,
    /// <summary>localityName (L), 2.5.4.7.</summary>
    LocalityName,
    /// <summary>streetAddress, 2.5.4.9.</summary>
    StreetAddress,
    /// <summary>postalCode, 2.5.4.17.</summary>
    PostalCode,
    /// <summary>serialNumber, 2.5.4.5.</summary>
    SerialNumber,
    /// <summary>title, 2.5.4.12.</summary>
    Title,
}
public static partial class Extensions
{
    /// <summary>
    /// Converts the <see cref="SubjectItemKind"/> enum value to its corresponding OID string representation.
    /// </summary>
    /// <param name="subjectItem">The <see cref="SubjectItemKind"/> value to convert.</param>
    /// <returns>The OID string representation of the <see cref="SubjectItemKind"/> value.</returns>
    public static string ToOidString(this SubjectItemKind subjectItem)
    {
        return subjectItem switch
        {
            SubjectItemKind.CommonName => OID.COMMON_NAME,
            SubjectItemKind.GivenName => OID.GIVEN_NAME,
            SubjectItemKind.Surname => OID.SURNAME,
            SubjectItemKind.CountryName => OID.COUNTRY_NAME,
            SubjectItemKind.OrganizationName => OID.ORGANIZATION_NAME,
            SubjectItemKind.OrganizationUnitName => OID.ORGANIZATION_UNIT,
            SubjectItemKind.StateOrProvinceName => OID.STATE_OR_PROVINCE_NAME,
            SubjectItemKind.LocalityName => OID.LOCALITY,
            SubjectItemKind.SerialNumber => OID.SERIAL_NUMBER,
            SubjectItemKind.Title => OID.TITLE,
            SubjectItemKind.OrganizationIdentifier => OID.ORGANIZATION_IDENTIFIER,
            SubjectItemKind.StreetAddress => OID.STREET_ADDRESS,
            SubjectItemKind.PostalCode => OID.POSTAL_CODE,
            _ => throw new ArgumentException($"Parameter '{nameof(subjectItem)}' has invalid value!", nameof(subjectItem)),
        };
    }

    /// <summary>
    /// Attempts to get the value for the requested subject item from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="subjectItem">The requested subject item.</param>
    /// <param name="forIssuer">Indicates whether to find the value for the requested subject item in the Issuer element or in the Subject element.</param>
    /// <param name="items">The list of values for the requested subject item.</param>
    /// <returns><see langword="true"/> if any value was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateSubjectItem(this ITag tag, SubjectItemKind subjectItem, bool forIssuer, out List<string> items)
    {
        return TryGetCertificateSubjectItem(tag, subjectItem.ToOidString(), forIssuer, out items);
    }

    /// <summary>
    /// Attempts to get the value for the requested subject item from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="oid">The OID of the requested subject item.</param>
    /// <param name="forIssuer">Indicates whether to find the value for the requested subject item in the Issuer element or in the Subject element.</param>
    /// <param name="items">The list of values for the requested subject item.</param>
    /// <returns><see langword="true"/> if any value was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateSubjectItem(this ITag tag, string oid, bool forIssuer, out List<string> items)
    {
        items = new List<string>();
        try
        {
            if (IsOidNullOrEmpty(oid) || !IsV3Certificate(tag))
                return false;
            // tbsCertificate: issuer is field 3, subject field 5
            var name = TbsFields(tag)[forIssuer ? 3 : 5];
            return InternalExtensions.TryGetSubjectItem(name, OidEncoder.GetBytes(oid), out items);
        }
        catch
        {
            items = new List<string>();
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value for the requested subject item from the specified certificate request tag.
    /// </summary>
    /// <param name="tag">The certificate request tag to check.</param>
    /// <param name="subjectItem">The requested subject item.</param>
    /// <param name="items">The list of values for the requested subject item.</param>
    /// <returns><see langword="true"/> if any value was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateRequestSubjectItem(this ITag tag, SubjectItemKind subjectItem, out List<string> items)
    {
        return TryGetCertificateRequestSubjectItem(tag, subjectItem.ToOidString(), out items);
    }

    /// <summary>
    /// Attempts to get the value for the requested subject item from the specified certificate request tag.
    /// </summary>
    /// <param name="tag">The certificate request tag to check.</param>
    /// <param name="oid">The OID of the requested subject item.</param>
    /// <param name="items">The list of values for the requested subject item.</param>
    /// <returns><see langword="true"/> if any value was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateRequestSubjectItem(this ITag tag, string oid, out List<string> items)
    {
        items = new List<string>();
        try
        {
            if (IsOidNullOrEmpty(oid) || !IsCertificationRequest(tag))
                return false;
            return InternalExtensions.TryGetSubjectItem(RequestSubject(tag), OidEncoder.GetBytes(oid), out items);
        }
        catch
        {
            items = new List<string>();
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value for the requested subject item from the specified certificate request tag.
    /// </summary>
    /// <remarks>
    /// Not all tags have content that can be converted to a human-readable string. Use this method with caution.
    /// If the tag is not a universal tag, the method will attempt to parse the content using the provided strategy locator. 
    /// Universal tags are parsed using the corresponding parsing strategy, which is resolved by <see cref="StrategyLocator"/>.
    /// </remarks>
    public static string ContentToString(this ITag tag, StrategyLocator? strategyLocator = null)
    {
        if (!tag.IsUniversal)
        {
            return OctetStringParsingStrategy.Default.Parse(tag.Content);
        }
        return (strategyLocator ?? StrategyLocator.Default).GetStrategy(tag.TagNumber & 0x1F)
                                                           .Parse(tag.Content);
    }
}
