﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace fASN1.NET;
public sealed class Asn1DeserializationException : Exception
{
    public Asn1DeserializationException()
    {
    }

    public Asn1DeserializationException(string message) : base(message)
    {
    }

    public Asn1DeserializationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
