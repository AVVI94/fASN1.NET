using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace fASN1.NET.Oid;

/// <summary>
/// Converts OBJECT IDENTIFIER values between the dotted text form (<c>1.2.840.113549</c>) and the content bytes
/// of their encoding (X.690 §8.19). Arcs may be arbitrarily large, as in the UUID arcs under <c>2.25</c>.
/// </summary>
public static class OidEncoder
{
    /// <summary>
    /// Converts a byte array representation of an OID to a string representation.
    /// </summary>
    /// <param name="oid">The byte array representing the OID.</param>
    /// <returns>The string representation of the OID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="oid"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">
    /// The bytes are not a valid encoding: the last arc is unterminated, or an arc has a leading 0x80 byte.
    /// </exception>
    public static string GetString(byte[] oid)
    {
        if (oid is null || oid.Length == 0)
        {
            throw new ArgumentNullException(nameof(oid));
        }
        if (!TryGetString(oid, out var value))
        {
            throw new ArgumentException("The bytes are not a valid OBJECT IDENTIFIER encoding.", nameof(oid));
        }
        return value;
    }

    /// <summary>
    /// Converts the content bytes of an OID to its dotted text form, or returns <see langword="false"/> when they
    /// are not a valid encoding.
    /// </summary>
    /// <param name="oid">The content bytes.</param>
    /// <param name="value">The dotted text form.</param>
    /// <returns><see langword="true"/> if the bytes are a valid encoding; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetString(byte[]? oid, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (oid is null || oid.Length == 0)
            return false;

        var text = new StringBuilder(oid.Length * 3);
        var pos = 0;
        while (pos < oid.Length)
        {
            if (!TryReadArc(oid, ref pos, out var small, out var big))
                return false;

            if (text.Length == 0)
            {
                // The first subidentifier packs the first two arcs as 40 * X + Y, where X is 0, 1 or 2 and Y is
                // below 40 unless X is 2; so values from 80 up are all under arc 2 (X.690 §8.19.4).
                if (big is null && small < 80)
                    text.Append(small / 40).Append('.').Append(small % 40);
                else if (big is null)
                    text.Append("2.").Append(small - 80);
                else
                    text.Append("2.").Append((big.Value - 80).ToString(CultureInfo.InvariantCulture));
            }
            else if (big is null)
            {
                text.Append('.').Append(small);
            }
            else
            {
                text.Append('.').Append(big.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        value = text.ToString();
        return true;
    }

    /// <summary>
    /// Converts a string representation of an OID to a byte array representation.
    /// </summary>
    /// <param name="oid">The string representation of the OID.</param>
    /// <returns>The byte array representation of the OID.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="oid"/> is null, empty, or whitespace, or is not a valid OID: fewer than two
    /// arcs, an arc that is not a decimal number without sign or leading zeros, a first arc above 2, or a second
    /// arc above 39 under the first arcs 0 and 1.
    /// </exception>
    public static byte[] GetBytes(string oid)
    {
        if (string.IsNullOrWhiteSpace(oid))
        {
            throw new ArgumentException($"'{nameof(oid)}' cannot be null or whitespace.", nameof(oid));
        }
        if (!TryGetBytes(oid, out var bytes))
        {
            throw new ArgumentException($"'{oid}' is not a valid OBJECT IDENTIFIER.", nameof(oid));
        }
        return bytes;
    }

    /// <summary>
    /// Converts the dotted text form of an OID to its content bytes, or returns <see langword="false"/> when the
    /// text is not a valid OID (see <see cref="GetBytes"/>).
    /// </summary>
    /// <param name="oid">The dotted text form.</param>
    /// <param name="bytes">The content bytes.</param>
    /// <returns><see langword="true"/> if the text is a valid OID; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetBytes(string? oid, [NotNullWhen(true)] out byte[]? bytes)
    {
        bytes = null;
        if (oid is null)
            return false;

        var arcs = oid.Split('.');
        if (arcs.Length < 2)
            return false;

        var values = new BigInteger[arcs.Length];
        for (int i = 0; i < arcs.Length; i++)
        {
            if (!TryParseArc(arcs[i], out values[i]))
                return false;
        }
        if (values[0] > 2 || (values[0] < 2 && values[1] > 39))
            return false;

        var result = new List<byte>(arcs.Length * 2);
        WriteArc(result, (values[0] * 40) + values[1]);
        for (int i = 2; i < values.Length; i++)
        {
            WriteArc(result, values[i]);
        }
        bytes = result.ToArray();
        return true;
    }

    /// <summary>Reads one base-128 subidentifier; <paramref name="big"/> is set when it does not fit a ulong.</summary>
    private static bool TryReadArc(byte[] bytes, ref int pos, out ulong small, out BigInteger? big)
    {
        small = 0;
        big = null;
        // A leading 0x80 adds nothing but a zero group, which X.690 §8.19.2 forbids.
        if (bytes[pos] == 0x80)
            return false;

        while (pos < bytes.Length)
        {
            var b = bytes[pos++];
            if (big is null && small > ulong.MaxValue >> 7)
                big = small;

            if (big is null)
                small = (small << 7) | (uint)(b & 0x7F);
            else
                big = (big.Value << 7) | (b & 0x7F);

            if ((b & 0x80) == 0)
                return true;
        }
        return false; // the last byte still had the continuation bit: the arc is unterminated
    }

    private static bool TryParseArc(string text, out BigInteger value)
    {
        value = default;
        if (text.Length == 0 || (text.Length > 1 && text[0] == '0'))
            return false;
        foreach (var c in text)
        {
            if (c is < '0' or > '9')
                return false;
        }
        value = BigInteger.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>Writes <paramref name="value"/> base 128, most significant group first, with the continuation bit on every byte but the last.</summary>
    private static void WriteArc(List<byte> destination, BigInteger value)
    {
        var start = destination.Count;
        do
        {
            destination.Add((byte)(value & 0x7F));
            value >>= 7;
        }
        while (!value.IsZero);
        destination.Reverse(start, destination.Count - start);
        for (int i = start; i < destination.Count - 1; i++)
        {
            destination[i] |= 0x80;
        }
    }
}
