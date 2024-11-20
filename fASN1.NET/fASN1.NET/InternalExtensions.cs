using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fASN1.NET.ContentParsing;
using fASN1.NET.Oid;
using fASN1.NET.Tags;

namespace fASN1.NET;

internal static class InternalExtensions
{
    public static readonly byte[] _version0Sequence = [0x00];
    public static readonly byte[] _version2Sequence = [0x02];
    public static readonly byte[] _booleanTrueSequence = [0xFF];
    public static readonly byte[] _keyUsageOidSequence = OID.GetOrCreate(OID.KEY_USAGE).ByteValue;
    public static readonly byte[] _sanOidSequence = OID.GetOrCreate(OID.SUBJECT_ALT_NAME).ByteValue;
    public static readonly byte[] _icaUserIdSequence = OID.GetOrCreate(OID.ICA_USER_ID).ByteValue;
    public static readonly byte[] _icaIkMpsvSequence = OID.GetOrCreate(OID.ICA_IK_MPSV).ByteValue;
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

    public static bool HasRequestedExtensions(ITag topLevelCertRequestTag)
    {
        return topLevelCertRequestTag.Children[0].Children[topLevelCertRequestTag.Children[0].Children.Count - 1].TagNumber == 160
                && topLevelCertRequestTag.Children[0].Children[topLevelCertRequestTag.Children[0].Children.Count - 1]
                              .Children[0].Children[0].Content.SequenceEqual(_extensionRequestSequence);
    }
    public static bool HasCertExtensionsInternal(ITag topLevelCertificateTag)
    {
        return topLevelCertificateTag.Children[0].Children[topLevelCertificateTag.Children[0].Children.Count - 1].TagNumber == 163;
    }

    public static IList<ITag> GetCretificateExtensionsInternal(ITag topLevelTag)
    {
        return topLevelTag.Children[0].Children[topLevelTag.Children[0].Children.Count - 1].Children[0].Children;
    }
    public static IList<ITag> GetRequestedExtensions(ITag topLevelTag)
    {
        return topLevelTag.Children[0].Children[topLevelTag.Children[0].Children.Count - 1].Children[0].Children[1].Children[0].Children;
    }

    public static bool IsKeyUsageSequence(this ITag tag)
    => tag.Children.Count == 3 && tag.Children[0] is { TagNumber: (int)Tag.ObjectIdentifier } oidTag && oidTag.Content.SequenceEqual(_keyUsageOidSequence);

    public static bool IsKeyUsageCritical(this ITag tag)
         => tag.IsKeyUsageSequence() && tag.Children[1] is { TagNumber: (int)Tag.Boolean } boolTag && boolTag.Content.SequenceEqual(_booleanTrueSequence);
    public static ITag GetKeyUsageBitStringTag(this ITag tag)
    => tag.Children[2].Children[0];
    public static string GetSetValue(ITag t)
    {
        return t.Children[0].Children[1].ContentToString();
    }
    public static bool IsSET(ITag t) => t.TagNumber == (int)Tag.Set;
    public static bool IsOidNullOrEmpty(string? oid)
    {
        return string.IsNullOrWhiteSpace(oid);
    }
    public static bool IsOidNullOrEmpty(byte[]? oid)
    {
        return oid is null or { Length: 0 };

    }

    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        if (list is List<T> l)
        {
            l.AddRange(items);
            return;
        }
        foreach (var item in items)
        {
            list.Add(item);
        }
    }

    public static int PeekByte(this Stream stream)
    {
        var value = stream.ReadByte();
        _ = stream.Seek(-1, SeekOrigin.Current);
        return value;
    }

    /// <summary>
    /// This method attempts to locate the value for the requested OID. It's important to note that a certificate may contain multiple values for the requested OID, and these values are ordered in the 'items' list as they appear in the structure.
    /// </summary>
    /// <remarks>
    /// The OID is matched based on X.509 v3 standards, which means that the OID is expected to have a parent element of type SEQUENCE, which, in turn, should have a parent element of type SET.
    /// The value is then extracted from the second element of the SEQUENCE element.<br/>
    /// Example of a correct SET element:
    /// <code>
    /// SET
    ///  | SEQUENCE
    ///  |  | OBJECT_IDENTIFIER 2.5.4.3, commonName, X.520 DN component
    ///  |  | UTF8String common name value
    /// </code>
    /// </remarks>
    /// <param name="tag">Parent tag</param>
    /// <param name="kind">Subject item kind</param>
    /// <param name="items">The value</param>
    /// <returns><see langword="true" /> if any value was found, otherwise <see langword="false" /></returns>
    public static bool TryGetSubjectItem(this ITag tag, SubjectItemKind kind, out List<string> items)
    {
        return TryGetSubjectItem(tag, OidEncoder.GetBytes(kind.ToOidString()), out items);
    }

    /// <summary>
    /// This method attempts to locate the value for the requested OID. It's important to note that a certificate may contain multiple values for the requested OID, and these values are ordered in the 'items' list as they appear in the structure.
    /// </summary>
    /// <remarks>
    /// The OID is matched based on X.509 v3 standards, which means that the OID is expected to have a parent element of type SEQUENCE, which, in turn, should have a parent element of type SET.
    /// The value is then extracted from the second element of the SEQUENCE element.<br/>
    /// Example of a correct SET element:
    /// <code>
    /// SET
    ///  | SEQUENCE
    ///  |  | OBJECT_IDENTIFIER 2.5.4.3, commonName, X.520 DN component
    ///  |  | UTF8String common name value
    /// </code>
    /// </remarks>
    /// <param name="tag">Parent tag</param>
    /// <param name="oid">Requested OID in byte array representation. <see cref="ASN1Decoder.NET.OidEncoder.OidEncoder"/> can be used to convert it from a string representation.</param>
    /// <param name="items">The value</param>
    /// <returns><see langword="true" /> if any value was found, otherwise <see langword="false" /></returns>
    public static bool TryGetSubjectItem(this ITag tag, byte[] oid, out List<string> items)
    {
        items = new List<string>();
        if (IsOidNullOrEmpty(oid))
            return false;
        /*
         SET
          | SEQUENCE
          |  | OBJECT_IDENTIFIER 2.5.4.3, commonName, X.520 DN component
          |  | UTF8String common name value
         */
        if (tag.Children.Count == 0)
            return false;
        try
        {
            if (IsValidSet(tag, oid))
            {
                items.Add(GetSetValue(tag));
            }
            foreach (var t in tag.Children)
            {
                bool isSet = IsSET(t);
                if (!isSet && t.Children.Count > 0 && TryGetSubjectItem(t, oid, out var items2))
                {
                    items.AddRange(items2);
                    continue;
                }
                if (IsValidSet(t, oid))
                {
                    items.Add(GetSetValue(t));
                }
            }
            return items.Count > 0;
        }
        catch
        {
            return false;
        }
        static bool IsValidSet(ITag t, byte[] oid)
            => IsSET(t)
               && t.Children.Count == 1
               && t.Children[0].TagNumber == (int)Tag.Sequence
               && t.Children[0].Children[0].Content.SequenceEqual(oid);
    }
}
