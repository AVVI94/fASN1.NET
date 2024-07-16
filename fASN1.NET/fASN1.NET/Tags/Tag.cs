using System;
using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public enum Tag
{
    Eoc = 0,
    Boolean = 0x01,
    Integer = 0x02,
    BitString = 0x03,
    OctetString = 0x04,
    Null = 0x05,
    ObjectIdentifier = 0x06,
    ObjectDescriptor = 0x07,
    External = 0x08,
    Real = 0x09,
    Enumerated = 0x0A,
    EmbeddedPDV = 0x0B,
    UTF8String = 0x0C,
    Sequence = 0x30,
    Set = 0x31,
    NumericString = 0x12,
    PrintableString = 0x13,
    TeletexString = 0x14,
    VideotexString = 0x15,
    IA5String = 0x16,
    UTCTime = 0x17,
    GeneralizedTime = 0x18,
    GraphicString = 0x19,
    VisibleString = 0x01A,
    GeneralString = 0x1B,
    UniversalString = 0x1C,
    CharacterString = 0x1D,
    BMPString = 0x1E,
}
