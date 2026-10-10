using System;
using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.SubjectDirectory;

/// <summary>The gender attribute of <see cref="SubjectDirectoryAttributes"/> (RFC 3739).</summary>
public enum Gender
{
    /// <summary>Encoded as <c>M</c>.</summary>
    Male,
    /// <summary>Encoded as <c>F</c>.</summary>
    Female
}
