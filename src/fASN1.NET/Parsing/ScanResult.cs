namespace fASN1.NET.Parsing;

/// <summary>The outcome of one step of the scan.</summary>
internal enum ScanResult
{
    /// <summary>An encoding was read completely and is valid.</summary>
    Succeeded,

    /// <summary>An encoding is invalid; the scanner's failure says why.</summary>
    Failed,

    /// <summary>The contents of an encoding are being read as nested encodings; they come next.</summary>
    ContentsEntered,

    /// <summary>The end-of-contents octets of the innermost indefinite-form encoding were read.</summary>
    EndOfContentsRead,
}
