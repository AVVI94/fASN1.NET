using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fASN1.NET.Oid;

namespace fASN1.NET;
/// <summary>
/// Represents the key usage of a certificate.
/// </summary>
public readonly struct KeyUsage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyUsage"/> struct.
    /// </summary>
    /// <param name="keyUsages">The array of key usages.</param>
    /// <param name="critical">A value indicating whether the key usage is critical.</param>
    /// <param name="decipherOnly">A value indicating whether the key is used only for deciphering.</param>
    internal KeyUsage(bool[] keyUsages, bool critical, bool decipherOnly) : this()
    {
        KeyUsages = keyUsages;
        Critical = critical;
        DecipherOnly = decipherOnly;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyUsage"/> struct with default values (all flags are set to <see langword="false"/>).
    /// </summary>
    /// <param name="critical">A value indicating whether the key usage is critical.</param>
    public KeyUsage(bool critical)
    {
        DecipherOnly = true;
        KeyUsages = new bool[8];
        Critical = critical;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyUsage"/> struct with specified key usages.
    /// </summary>
    /// <param name="critical">A value indicating whether the key usage is critical.</param>
    /// <param name="digitalSignature">A value indicating whether the key is used for digital signature.</param>
    /// <param name="nonRepudiation">A value indicating whether the key is used for non-repudiation.</param>
    /// <param name="keyEncipherment">A value indicating whether the key is used for key encipherment.</param>
    /// <param name="dataEncipherment">A value indicating whether the key is used for data encipherment.</param>
    /// <param name="keyAgreement">A value indicating whether the key is used for key agreement.</param>
    /// <param name="keyCertSign">A value indicating whether the key is used for key certificate signing.</param>
    /// <param name="cRLSign">A value indicating whether the key is used for CRL signing.</param>
    /// <param name="encipherOnly">A value indicating whether the key is used only for enciphering.</param>
    public KeyUsage(bool critical,
                    bool digitalSignature = false,
                    bool nonRepudiation = false,
                    bool keyEncipherment = false,
                    bool dataEncipherment = false,
                    bool keyAgreement = false,
                    bool keyCertSign = false,
                    bool cRLSign = false,
                    bool encipherOnly = false)
    {
        Critical = critical;
        KeyUsages =
        [
            digitalSignature,
            nonRepudiation,
            keyEncipherment,
            dataEncipherment,
            keyAgreement,
            keyCertSign,
            cRLSign,
            encipherOnly,
        ];
        DecipherOnly = true;
        if (digitalSignature
            || nonRepudiation
            || keyEncipherment
            || dataEncipherment
            || keyAgreement
            || keyCertSign
            || cRLSign
            || encipherOnly)
            DecipherOnly = false;
    }

    /// <summary>
    /// Gets the array of key usages. Each element represents one bit in the key usage byte.
    /// </summary>
    public readonly bool[]? KeyUsages { get; }

    /// <summary>
    /// Gets the byte value representing the key usage.
    /// </summary>
    public byte KeyUsageByteValue
    {
        get
        {
            if (KeyUsages is null)
                return 0;
            byte result = 0;

            for (int i = 0; i < 8; i++)
            {
                if (KeyUsages[i])
                {
                    result |= (byte)(1 << 7 - i);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the key is used for digital signature.
    /// </summary>
    public readonly bool DigitalSignature => KeyUsages?[0] ?? false;

    /// <summary>
    /// Gets a value indicating whether the key is used for non-repudiation.
    /// </summary>
    public readonly bool NonRepudiation => KeyUsages?[1] ?? false;

    /// <summary>
    /// Gets a value indicating whether the key is used for key encipherment.
    /// </summary>
    public readonly bool KeyEncipherment => KeyUsages?[2] ?? false;

    /// <summary>
    /// Gets a value indicating whether the key is used for data encipherment.
    /// </summary>
    public readonly bool DataEncipherment => KeyUsages?[3] ?? false;

    /// <summary>
    /// Gets a value indicating whether the key is used for key agreement.
    /// </summary>
    public readonly bool KeyAgreement => KeyUsages?[4] ?? false;

    /// <summary>
    /// Gets a value indicating whether the key is used for code signing.
    /// </summary>
    public readonly bool KeyCertSign => KeyUsages?[5] ?? false;

    /// <summary>
    /// Gets a value indicating whether the key is used for CRL signing.
    /// </summary>
    public readonly bool CRLSign => KeyUsages?[6] ?? false;

    /// <summary>
    /// Gets a value indicating whether the key is used only for enciphering.
    /// </summary>
    public readonly bool EncipherOnly => KeyUsages?[7] ?? false;

    /// <summary>
    /// Gets a value indicating whether the key is used only for deciphering.
    /// </summary>
    public readonly bool DecipherOnly { get; }

    /// <summary>
    /// Gets a value indicating whether the key usage is critical.
    /// </summary>
    public readonly bool Critical { get; }

    /// <summary>
    /// Returns a string that represents the current <see cref="KeyUsage"/>.
    /// </summary>
    /// <returns>A string that represents the current <see cref="KeyUsage"/>.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();
        if (DigitalSignature)
            _ = sb.Append(nameof(DigitalSignature)).Append(" ");
        if (NonRepudiation)
            _ = sb.Append(nameof(NonRepudiation)).Append(" ");
        if (KeyEncipherment)
            _ = sb.Append(nameof(KeyEncipherment)).Append(" ");
        if (DataEncipherment)
            _ = sb.Append(nameof(DataEncipherment)).Append(" ");
        if (KeyAgreement)
            _ = sb.Append(nameof(KeyAgreement)).Append(" ");
        if (KeyCertSign)
            _ = sb.Append(nameof(KeyCertSign)).Append(" ");
        if (CRLSign)
            _ = sb.Append(nameof(CRLSign)).Append(" ");
        if (EncipherOnly)
            _ = sb.Append(nameof(EncipherOnly)).Append(" ");
        if (DecipherOnly)
            _ = sb.Append(nameof(DecipherOnly)).Append(" ");
        return sb.ToString().Trim();
    }
}

/// <summary>
/// Represents the extended key usage of a certificate.
/// </summary>
public struct ExtendedKeyUsage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedKeyUsage"/> struct.
    /// </summary>
    /// <param name="clientAuth">A value indicating whether the key is used for client authentication.</param>
    /// <param name="serverAuth">A value indicating whether the key is used for server authentication.</param>
    /// <param name="codeSigning">A value indicating whether the key is used for code signing.</param>
    /// <param name="emailProtection">A value indicating whether the key is used for email protection.</param>
    /// <param name="timeStamping">A value indicating whether the key is used for timestamping.</param>
    /// <param name="ocspSigning">A value indicating whether the key is used for OCSP signing.</param>
    public ExtendedKeyUsage(bool clientAuth = false,
                           bool serverAuth = false,
                           bool codeSigning = false,
                           bool emailProtection = false,
                           bool timeStamping = false,
                           bool ocspSigning = false) : this()
    {
        ClientAuth = clientAuth;
        ServerAuth = serverAuth;
        CodeSigning = codeSigning;
        EmailProtection = emailProtection;
        TimeStamping = timeStamping;
        OcspSigning = ocspSigning;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the key is used for client authentication.
    /// </summary>
    public bool ClientAuth { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the key is used for server authentication.
    /// </summary>
    public bool ServerAuth { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the key is used for code signing.
    /// </summary>
    public bool CodeSigning { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the key is used for email protection.
    /// </summary>
    public bool EmailProtection { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the key is used for timestamping.
    /// </summary>
    public bool TimeStamping { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the key is used for OCSP signing.
    /// </summary>
    public bool OcspSigning { get; internal set; }

    /// <summary>
    /// Gets or sets the list of other extended key usages.
    /// </summary>
    public IReadOnlyList<OID>? OtherEKUs { get; internal set; }
}
