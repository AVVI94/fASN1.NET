namespace fASN1.NET;

/// <summary>
/// Represents an I.CA Certificate Interconnection structure.
/// </summary>
public readonly record struct ICACertIntercon
{
    /// <summary>
    /// Creates a new instance of the I.CA Certificate Interconnection
    /// </summary>
    /// <param name="isMaster">Specifies if the certificate this structure was parsed from is a master certificate</param>
    /// <param name="masterRequestId">The master's certificate request ID</param>
    /// <param name="interconnectedCertificatesCount">The number of certificates that are interconnected</param>
    public ICACertIntercon(bool isMaster, string masterRequestId, int interconnectedCertificatesCount)
    {
        IsMaster = isMaster;
        MasterRequestId = masterRequestId;
        InterconnectedCertificatesCount = interconnectedCertificatesCount;
    }

    /// <summary>
    /// Indicating whether the certificate is a master certificate
    /// </summary>
    public readonly bool IsMaster { get; }

    /// <summary>
    /// The master's certificate request ID
    /// </summary>
    public readonly string MasterRequestId { get; }

    /// <summary>
    /// The number of certificates that are interconnected
    /// </summary>
    public readonly int InterconnectedCertificatesCount { get; }
}