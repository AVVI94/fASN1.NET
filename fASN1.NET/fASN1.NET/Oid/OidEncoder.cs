using System;
using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Oid;

/// <summary>
/// Provides methods for encoding and decoding Object Identifier (OID) values.
/// </summary>
public static class OidEncoder
{
    /// <summary>
    /// Converts a byte array representation of an OID to a string representation.
    /// </summary>
    /// <param name="oid">The byte array representing the OID.</param>
    /// <returns>The string representation of the OID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="oid"/> is null.</exception>
    public static string GetString(byte[] oid)
    {
        if (oid is null || oid.Length == 0)
        {
            throw new ArgumentNullException(nameof(oid));
        }

        var oidString = ConvertBytesToOidString(oid);

        return oidString;
    }

    /// <summary>
    /// Converts a string representation of an OID to a byte array representation.
    /// </summary>
    /// <param name="oid">The string representation of the OID.</param>
    /// <returns>The byte array representation of the OID.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="oid"/> is null, empty, or whitespace, or when the OID has less than two components.</exception>
    public static byte[] GetBytes(string oid)
    {
        if (string.IsNullOrWhiteSpace(oid))
        {
            throw new ArgumentException($"'{nameof(oid)}' cannot be null or whitespace.", nameof(oid));
        }

        var split = oid.Split('.');
        if (split.Length < 2)
        {
            throw new ArgumentException("OID must have at least two components.");
        }

        int int1 = int.Parse(split[0]);
        int int2 = int.Parse(split[1]);

        List<byte> res = [(byte)(40 * int1 + int2)];

        for (int i = 2; i < split.Length; i++)
        {
            var x = long.Parse(split[i]);

            int numBytes = 1;
            long temp = x;

            // Calculate the number of bytes needed
            while ((temp >>= 7) > 0)
            {
                numBytes++;
            }

            // Insert bytes from the end
            for (int j = numBytes - 1; j >= 0; j--)
            {
                byte b = (byte)((x >> (7 * j)) & 0x7F);
                if (j != 0)
                {
                    b |= 0x80; // Set the MSB of each byte except the last one
                }
                res.Add(b);
            }
        }

        return [.. res];
    }

    private static string ConvertBytesToOidString(byte[] bytes)
    {
        StringBuilder oid = new();
        int bufferValue = 0;
        bool isBuffering = false;

        // The numerical value of the first subidentifier is derived from the values of the first two object identifier
        // components in the object identifier value being encoded, using the formula:
        // (X * 40) + Y
        // where X is the value of the first object identifier component and Y is the value of the second object identifier
        // component.
        // NOTE – This packing of the first two object identifier components recognizes that only three values are allocated from the root
        // node, and at most 39 subsequent values from nodes reached by X = 0 and X = 1.
        int firstByte = bytes[0];
        if (firstByte > 79)
        {
            _ = oid.Append(2)
                   .Append('.')
                   .Append(firstByte - 80);
        }
        else
        {
            _ = oid.Append(firstByte / 40)
                   .Append('.')
                   .Append(firstByte % 40);
        }

        for (int i = 1; i < bytes.Length; i++)
        {
            int currentByte = bytes[i];

            if (currentByte > 127)
            {
                // If the current byte is part of a multi-byte number, accumulate its value
                bufferValue = (bufferValue << 7) | (currentByte & 0x7F);
                isBuffering = true;
            }
            else
            {
                if (isBuffering)
                {
                    // If we were buffering, this byte is the last part of the multi-byte number
                    bufferValue = (bufferValue << 7) | currentByte;
                    _ = oid.Append('.')
                           .Append(bufferValue);
                    bufferValue = 0;
                    isBuffering = false;
                }
                else
                {
                    // Single byte number, directly append to OID
                    _ = oid.Append('.')
                           .Append(currentByte);
                }
            }
        }

        return oid.ToString();
    }
}
