using System;
using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// Represents an ASN.1 tag.
/// </summary>
public interface ITag
{
    /// <summary>
    /// Gets the tag number.
    /// </summary>
    int TagNumber { get; }

    /// <summary>
    /// Gets the tag name.
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// Gets the tag class.
    /// </summary>
    int TagClass { get; }

    /// <summary>
    /// Gets a value indicating whether the tag is constructed.
    /// </summary>
    bool IsConstructed { get; }

    /// <summary>
    /// Gets a value indicating whether the tag is universal.
    /// </summary>
    bool IsUniversal { get; }

    /// <summary>
    /// Gets a value indicating whether the tag is an end-of-content tag.
    /// </summary>
    bool IsEoc { get; }

    /// <summary>
    /// Gets the children tags.
    /// </summary>
    IList<ITag> Children { get; }

    /// <summary>
    /// Gets or sets the content of the tag.
    /// </summary>
    byte[] Content { get; set; }

    /// <summary>
    /// Gets or sets the tag in the children list.
    /// </summary>
    /// <param name="index">Index of the item in children list.</param>
    /// <returns>The tag at the specified index.</returns>
    /// <throws cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</throws>
    ITag this[int index] { get; set; }
}
