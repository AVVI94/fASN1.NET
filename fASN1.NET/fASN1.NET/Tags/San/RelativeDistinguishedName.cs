using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fASN1.NET.Oid;

namespace fASN1.NET.Tags.San;
/// <summary>
/// Represents a relative distinguished name.
/// </summary>
public class RelativeDistinguishedName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeDistinguishedName"/> class.
    /// </summary>
    /// <param name="oid">The object identifier.</param>
    /// <param name="value">The value.</param>
    public RelativeDistinguishedName(OID oid, string value)
    {
        ObjectIdentifier = oid;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeDistinguishedName"/> class.
    /// </summary>
    public RelativeDistinguishedName()
    {
    }

    /// <summary>
    /// Gets or sets the object identifier.
    /// </summary>
    public OID? ObjectIdentifier { get; protected set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string? Value { get; protected set; }
}
