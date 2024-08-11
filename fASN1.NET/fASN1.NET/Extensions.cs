using System;
using System.Collections.Generic;
using System.Linq;
using fASN1.NET.Oid;
using fASN1.NET.Tags;
using fASN1.NET.Tags.San;
using static fASN1.NET.InternalExtensions;
using IPAddress = fASN1.NET.Tags.San.IPAddress;

namespace fASN1.NET;

public static partial class Extensions
{
    /// <summary>
    /// Determines whether the specified tag represents a certificate request.
    /// </summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns><see langword="true"/> if the tag represents a certificate request; otherwise, <see langword="false"/>.</returns>
    public static bool IsCertificateRequest(this ITag tag)
    {
        try
        {
            return tag.Children.Count == 3
               && tag.Children[0].Children.Count != 0
               && tag.Children[0].Children[0].TagNumber == (int)Tag.Integer
               && tag.Children[0].Children[0].Content.SequenceEqual(_version0Sequence)
               && tag.Children[1].TagNumber == (int)Tag.Sequence
               && (tag.Children[1].Children.Count == 1 || tag.Children[1].Children.Count == 2)
               && tag.Children[1].Children[0].TagNumber == (int)Tag.ObjectIdentifier
               && tag.Children[2].Children.Count == 0
               && tag.Children[2].TagNumber == (int)Tag.BitString;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether the specified tag represents a X.509v3 certificate.
    /// </summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns><see langword="true"/> if the tag represents a X.509v3 certificate; otherwise, <see langword="false"/>.</returns>
    public static bool IsCertificate(this ITag tag)
    {
        try
        {
            return tag.Children.Count == 3
                       && tag.Children[0].Children.Count != 0
                       && tag.Children[0].Children[0].TagClass == 2
                       && tag.Children[0].Children[0].Children[0].TagNumber == (int)Tag.Integer
                       && tag.Children[0].Children[0].Children[0].Content.SequenceEqual(_version2Sequence)
                       && tag.Children[1].TagNumber == (int)Tag.Sequence
                       && (tag.Children[1].Children.Count == 1 || tag.Children[1].Children.Count == 2)
                       && tag.Children[1].Children[0].TagNumber == (int)Tag.ObjectIdentifier
                       && tag.Children[2].Children.Count == 0
                       && tag.Children[2].TagNumber == (int)Tag.BitString;
        }
        catch
        {
            return false;
        }
    }

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
            if (!IsCertificate(certificateTag) || !HasCertExtensions(certificateTag))
                return false;
            bool ekuFound = false;
            foreach (var ext in GetCretificateExtensions(certificateTag))
            {
                if (!ext.Children[0].Content.SequenceEqual(_extKeyUsageSequence))
                    continue;
                foreach (var e in ext.Children[1].Children[0].Children)
                {
                    switch (OID.GetOrCreate(e.Content).Value)
                    {
                        case OID.EKU_EMAIL_PROTECTION:
                            eku.EmailProtection = true;
                            ekuFound = true;
                            break;
                        case OID.EKU_CLIENT_AUTH:
                            eku.ClientAuth = true;
                            ekuFound = true;
                            break;
                        case OID.EKU_SERVER_AUTH:
                            eku.ServerAuth = true;
                            ekuFound = true;
                            break;
                        case OID.EKU_TIME_STAMPING:
                            eku.TimeStamping = true;
                            ekuFound = true;
                            break;
                        case OID.EKU_CODE_SIGNING:
                            eku.CodeSigning = true;
                            ekuFound = true;
                            break;
                        case OID.EKU_OCSP_SIGNING:
                            eku.OcspSigning = true;
                            ekuFound = true;
                            break;
                        default:
                            eku.OtherEKUs ??= new List<OID>();
                            ((List<OID>)eku.OtherEKUs).Add(OID.GetOrCreate(e.Content));
                            ekuFound = true;
                            break;
                    }
                }
            }
            return ekuFound;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryGetICACertIntercon(this ITag certificate, out ICACertIntercon intercon)
    {
        intercon = default;
        if (!IsCertificate(certificate) || !HasCertExtensions(certificate))
            return false;
        var ext = GetCretificateExtensions(certificate);
        foreach (var item in ext)
        {
            if (OID.GetOrCreate(item.Children[0].Content).Value != OID.ICA_CERT_INTERCONNECTION)
                continue;
            var masterReqId = item.Children[1].Children[0].Children[0];
            var certCount = item.Children[1].Children[0].Children[1];
            intercon = new ICACertIntercon(item.Children[1].Children[0].Children[2].Content.SequenceEqual(_booleanTrueSequence),
                                           masterReqId.ContentToString(),
                                           int.Parse(certCount.ContentToString()));
            return true;
        }
        return false;
    }


    /// <summary>
    /// Attempts to get the key usage from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="keyUsage">The key usage object.</param>
    /// <returns><see langword="true"/> if the key usage was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetKeyUsage(this ITag tag, out KeyUsage keyUsage)
    {
        keyUsage = default;
        try
        {
            //if the tag is cert, the KeyUsage location is somewhere in cert's extensions
            if (IsCertificate(tag))
                tag = tag.Children[0].Children[tag.Children[0].Children.Count - 1].Children[0];
            return FindKeyUsage(tag, ref keyUsage);
        }
        catch
        {
            return false;
        }

        static bool FindKeyUsage(ITag tag, ref KeyUsage keyUsage)
        {
            if (tag.IsKeyUsageSequence())
            {
                var ku = tag.GetKeyUsageBitStringTag();
                var b = ku.Content[0];
                bool[] kus = [
                    ((b >> 1) & 1) == 1,
                    ((b >> 2) & 1) == 1,
                    ((b >> 3) & 1) == 1,
                    ((b >> 4) & 1) == 1,
                    ((b >> 5) & 1) == 1,
                    ((b >> 6) & 1) == 1,
                    ((b >> 7) & 1) == 1,
                    ((b >> 8) & 1) == 1,
                    ];
                keyUsage = new KeyUsage(kus, tag.IsKeyUsageCritical(), kus[7]);
                return true;
            }

            foreach (var t in tag.Children)
            {
                if (FindKeyUsage(t, ref keyUsage))
                    return true;
            }
            return false;
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
            if (!IsCertificate(tag))
                return false;
            serialNumber = tag.Children[0].Children[1].ContentToString();
            return serialNumber is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the subject alternative name (SAN) from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="san">The list of subject alternative name items.</param>
    /// <returns><see langword="true"/> if the subject alternative name was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetSubjectAlternativeName(this ITag tag, out List<ISanItem> san)
    {
        san = new();
        try
        {
            if (IsCertificate(tag))
            {
                if (!HasCertExtensions(tag))
                    return false;
                foreach (var item in GetCretificateExtensions(tag))
                {
                    if (!item.Children[0].Content.SequenceEqual(_sanOidSequence))
                        continue;
                    ParseSan(san, item);
                    break;
                }
            }
            else if (IsCertificateRequest(tag))
            {
                if (!HasRequestedExtensions(tag))
                    return false;
                foreach (var item in GetRequestedExtensions(tag))
                {
                    if (!item.Children[0].Content.SequenceEqual(_sanOidSequence))
                        continue;
                    ParseSan(san, item);
                    break;
                }
            }
            return true;

        }
        catch
        {
            return false;
        }

        static void ParseSan(List<ISanItem> san, ITag item)
        {
            foreach (var sanItem in item.Children[1].Children[0].Children)
            {
                switch (sanItem.TagName)
                {
                    case "[0]":
                        if (sanItem.Children[0].Content.SequenceEqual(_icaUserIdSequence))
                            san.Add(new IcaUserId(sanItem.Children[1]));
                        else if (sanItem.Children[0].Content.SequenceEqual(_icaIkMpsvSequence))
                            san.Add(new IcaIkMpsv(sanItem.Children[1]));
                        else
                            san.Add(new OtherName(OID.GetOrCreate(sanItem.Children[0].Content), sanItem.Children[1].Children[0]));
                        break;
                    case "[1]":
                        san.Add(new Rfc822Name(sanItem.ContentToString()));
                        break;
                    case "[2]":
                        san.Add(new DnsName(sanItem.ContentToString()));
                        break;
                    case "[3]":
                        san.Add(new X400Address(sanItem.Children[0]));
                        break;
                    case "[4]":
                        san.Add(new DirectoryName(sanItem.Children[0]));
                        break;
                    case "[5]":
                        san.Add(new EdiPartyName(sanItem.Children[0]));
                        break;
                    case "[6]":
                        san.Add(new UniformResourceIdentifier(sanItem.ContentToString()));
                        break;
                    case "[7]":
                        san.Add(new IPAddress(sanItem.ContentToString()));
                        break;
                    case "[8]":
                        san.Add(new RegisteredID(OID.GetOrCreate(sanItem.Children[0].Content)));
                        break;
                    default:
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Attempts to get the notAfter date from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="notAfter">The notAfter date of the certificate.</param>
    /// <returns><see langword="true"/> if the notAfter date was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateNotAfter(this ITag tag, out DateTime notAfter)
    {
        notAfter = default;
        try
        {
            if (!IsCertificate(tag))
                return false;
            notAfter = DateTime.Parse(tag.Children[0].Children[4].Children[1].ContentToString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the notBefore date from the specified X.509v3 certificate tag.
    /// </summary>
    /// <param name="tag">The certificate tag to check.</param>
    /// <param name="notBefore">The notBefore date of the certificate.</param>
    /// <returns><see langword="true"/> if the notBefore date was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCertificateNotBefore(this ITag tag, out DateTime notBefore)
    {
        notBefore = default;
        try
        {
            if (!IsCertificate(tag))
                return false;
            notBefore = DateTime.Parse(tag.Children[0].Children[4].Children[0].ContentToString());
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public enum SubjectItemKind
{
    CommonName,
    GivenName,
    Surname,
    CountryName,
    OrganizationName,
    OrganizationUnitName,
    OrganizationIdentifier,
    StateOrProvinceName,
    LocalityName,
    StreetAddress,
    PostalCode,
    SerialNumber,
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
        if (IsOidNullOrEmpty(oid) || !IsCertificate(tag))
            return false;

        if (forIssuer)
        {
            return InternalExtensions.TryGetSubjectItem(tag.Children[0].Children[3], OidEncoder.GetBytes(oid), out items);
        }
        else
        {
            return InternalExtensions.TryGetSubjectItem(tag.Children[0].Children[5], OidEncoder.GetBytes(oid), out items);
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
            if (IsOidNullOrEmpty(oid) || !IsCertificateRequest(tag))
                return false;
            tag = tag.Children[0].Children[1];
            return InternalExtensions.TryGetSubjectItem(tag, OidEncoder.GetBytes(oid), out items);
        }
        catch
        {
            return false;
        }
    }
}
