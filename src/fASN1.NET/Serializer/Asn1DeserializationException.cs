using System;

namespace fASN1.NET;

/// <summary>
/// Thrown by <see cref="Asn1Serializer.Deserialize(byte[])"/> and its overloads when the input is not valid ASN.1.
/// The message says what is wrong and at which offset.
/// </summary>
public sealed class Asn1DeserializationException : Exception
{
    /// <summary>Creates the exception without a message.</summary>
    public Asn1DeserializationException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What is wrong with the input.</param>
    public Asn1DeserializationException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the exception that caused it.</summary>
    /// <param name="message">What is wrong with the input.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public Asn1DeserializationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
