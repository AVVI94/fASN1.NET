using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using fASN1.NET.ContentParsing;
using fASN1.NET.Tags;

namespace fASN1.NET.SubjectDirectory;
/// <summary>
/// Represents the subject directory attributes.
/// </summary>
public class SubjectDirectoryAttributes
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubjectDirectoryAttributes"/> class.
    /// </summary>
    public SubjectDirectoryAttributes()
    {
        CountryOfResidence = CountryOfCitizenship = [];
        IsEmpty = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubjectDirectoryAttributes"/> class with specified parameters.
    /// </summary>
    /// <param name="gender">The gender of the subject.</param>
    /// <param name="genderString">The gender of the subject as a string.</param>
    /// <param name="dateOfBirth">The date of birth of the subject.</param>
    /// <param name="placeOfBirth">The place of birth of the subject.</param>
    /// <param name="countryOfResidence">The country of residence of the subject.</param>
    /// <param name="countryOfCitizenship">The country of citizenship of the subject.</param>
    public SubjectDirectoryAttributes(Gender? gender,
                                      string? genderString,
                                      DateTime? dateOfBirth,
                                      string? placeOfBirth,
                                      IReadOnlyList<string> countryOfResidence,
                                      IReadOnlyList<string> countryOfCitizenship)
    {
        Gender = gender;
        GenderString = genderString;
        DateOfBirth = dateOfBirth;
        PlaceOfBirth = placeOfBirth;
        CountryOfResidence = countryOfResidence;
        CountryOfCitizenship = countryOfCitizenship;

        IsEmpty = gender is null
                    && string.IsNullOrEmpty(genderString)
                    && dateOfBirth is null
                    && string.IsNullOrEmpty(placeOfBirth)
                    && countryOfResidence.Count == 0
                    && countryOfCitizenship.Count == 0;
    }

    /// <summary>
    /// Gets a value indicating whether the subject directory attributes are empty.
    /// </summary>
    public bool IsEmpty { get; protected set; }

    /// <summary>
    /// Gets the gender. 
    /// </summary>
    /// <remarks>
    /// If this property is <see langword="null"/>, you may use <see cref="GenderString"/> property to get value that is not in the enum range.
    /// </remarks>
    public Gender? Gender { get; protected set; }

    /// <summary>
    /// Gets the gender as a string.
    /// </summary>
    public string? GenderString { get; protected set; }

    /// <summary>
    /// Gets the date of birth.
    /// </summary>
    public DateTime? DateOfBirth { get; protected set; }

    /// <summary>
    /// Gets the place of birth.
    /// </summary>
    public string? PlaceOfBirth { get; protected set; }

    /// <summary>
    /// Gets the country of residence.
    /// </summary>
    public IReadOnlyList<string> CountryOfResidence { get; protected set; }

    /// <summary>
    /// Gets the country of citizenship.
    /// </summary>
    public IReadOnlyList<string> CountryOfCitizenship { get; protected set; }

    /// <summary>
    /// Gets an empty instance of the <see cref="SubjectDirectoryAttributes"/> class.
    /// </summary>
    public static SubjectDirectoryAttributes Empty { get; } = new();

    /// <summary>
    /// Creates a new instance of the <see cref="SubjectDirectoryAttributes"/> class from the specified tag.
    /// </summary>
    /// <param name="sda">SubjectDirectoryAttributes content tag</param>
    /// <param name="strategyLocator">Optional parameter for custom implememntation of <see cref="StrategyLocator"/></param>
    /// <returns>
    /// Parsed <see cref="SubjectDirectoryAttributes"/> object or <see cref="SubjectDirectoryAttributes.Empty" /> if the SDA could not be parsed.
    /// </returns>
    public static SubjectDirectoryAttributes? FromTag(ITag sda, StrategyLocator? strategyLocator = null)
    {
        if (sda.IsCertificate())
        {
            sda = sda.GetSubjectDirectoryAttributesRaw()!;
        }
        if (sda is null)
        {
            return Empty;
        }
        _ = strategyLocator ?? StrategyLocator.Default;

        string? gender = null;
        string? dateOfBirth = null;
        string? placeOfBirth = null;
        List<string> countryOfResidence = [];
        List<string> countryOfCitizenship = [];

        if (sda.Children.Count == 0)
            return Empty;

        //sdaItem should be a SEQUENCE
        foreach (var sdaItem in sda.Children)
        {
            var oid = sdaItem[0].Content.AsSpan();
            var value = sdaItem[1][0].ContentToString();

            if (oid.SequenceEqual(InternalExtensions._genderOidSequence))
            {
                gender = value;
            }
            else if (oid.SequenceEqual(InternalExtensions._dateOfBirthOidSequence))
            {
                dateOfBirth = value;
            }
            else if (oid.SequenceEqual(InternalExtensions._placeOfBirthOidSequence))
            {
                placeOfBirth = value;
            }
            else if (oid.SequenceEqual(InternalExtensions._countryOfResidenceOidSequence))
            {
                countryOfResidence.Add(value);
            }
            else if (oid.SequenceEqual(InternalExtensions._countryOfCitizenshipOidSequence))
            {
                countryOfCitizenship.Add(value);
            }
        }

        DateTime? dateOfBirthDate = null;
        if (DateTime.TryParse(dateOfBirth, out var v))
        {
            dateOfBirthDate = v.Date;
        }

        var attrs = new SubjectDirectoryAttributes(
            gender switch
            {
                "M" or "m" => SubjectDirectory.Gender.Male,
                "F" or "f" => SubjectDirectory.Gender.Female,
                _ => null
            },
            gender,
            dateOfBirthDate,
            placeOfBirth,
            countryOfResidence,
            countryOfCitizenship
        );

        return attrs;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SubjectDirectoryAttributes"/> class from the specified certificate tag.
    /// </summary>
    /// <param name="certificateTag">
    /// The certificate tag.
    /// </param>
    /// <returns>
    /// Parsed <see cref="SubjectDirectoryAttributes"/> object or <see cref="SubjectDirectoryAttributes.Empty" /> if the SDA could not be parsed.
    /// </returns>
    public static SubjectDirectoryAttributes? FromCertificate(ITag certificateTag)
        => FromTag(certificateTag);

    /// <summary>
    /// Creates a new instance of the <see cref="SubjectDirectoryAttributes"/> class from the specified certificate.
    /// </summary>
    /// <param name="x509Certificate">The certificate.</param>
    /// <returns>
    /// Parsed <see cref="SubjectDirectoryAttributes"/> object or <see cref="SubjectDirectoryAttributes.Empty" /> if the SDA could not be parsed.
    /// </returns>
    public static SubjectDirectoryAttributes? FromCertificate(X509Certificate2 x509Certificate)
    {
        foreach (X509Extension extension in x509Certificate.Extensions)
        {
            if (Oid.OID.GetOrCreate(extension.Oid.Value).ByteValue
                       .AsSpan()
                       .SequenceEqual(InternalExtensions._subjectDirectoryAttributesOidSequence))
            {
                using var ms = new MemoryStream(extension.RawData);
                var tag = Asn1Serializer.Deserialize(ms);
                return FromTag(tag);
            }
        }
        return Empty;
    }
}