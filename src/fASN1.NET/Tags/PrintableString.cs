using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 PrintableString type (universal 19): letters, digits, space and <c>' ( ) + , - . / : = ?</c>.
/// </summary>
public class PrintableString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public PrintableString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }
    /// <summary>Creates the tag from text, encoded as ASCII.</summary>
    /// <param name="text">Characters allowed in a PrintableString.</param>
    /// <exception cref="System.ArgumentException">The text contains a character the type does not allow.</exception>
    public PrintableString(string text) : this(GetBytes(text))
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => 19;
    /// <inheritdoc/>
    public override string TagName => "PrintableString";
    /// <inheritdoc/>
    public override int TagClass => 0;

    /// <summary>Checks that every character of <paramref name="input"/> is allowed in a PrintableString.</summary>
    /// <param name="input">The text to check.</param>
    /// <exception cref="ArgumentException">A character is not allowed; the message names it and its index.</exception>
    public static void EnsureValidValue(string input)
    {
        // Check each character in the input string
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (!IsAllowedCharacter(c))
            {
                throw new ArgumentException($"Invalid character '{c}' at index {i}. See ASN.1 PrintableString definition!", "value");
            }
        }
    }

    private static bool IsAllowedCharacter(char c)
    {
        // Check if the character is within the allowed ASCII ranges for PrintableString
        return c >= 'A' && c <= 'Z' || // Latin capital letters
               c >= 'a' && c <= 'z' || // Latin small letters
               c >= '0' && c <= '9' || // Numbers
               c == ' ' || // SPACE
               c == '\'' || // APOSTROPHE
               c == '(' || // LEFT PARENTHESIS
               c == ')' || // RIGHT PARENTHESIS
               c == '+' || // PLUS SIGN
               c == ',' || // COMMA
               c == '-' || // HYPHEN-MINUS
               c == '.' || // FULL STOP
               c == '/' || // SOLIDUS
               c == ':' || // COLON
               c == '=' || // EQUALS SIGN
               c == '?';   // QUESTION MARK
    }

    private static byte[] GetBytes(string input)
    {
        EnsureValidValue(input);
        return Encoding.ASCII.GetBytes(input);
    }
}
