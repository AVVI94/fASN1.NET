using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class PrintableString : TagBase
{
    public PrintableString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }
    public PrintableString(string text) : this(GetBytes(text))
    {
    }
    public override int TagNumber => 19;
    public override string TagName => "PrintableString";
    public override int TagClass => 0;

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
