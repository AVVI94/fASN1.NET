using System;
using System.Collections.Generic;
 

namespace fASN1.NET.Tags;

public class PrintableString : ITag
{
    public PrintableString(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public int TagNumber { get; } = 19;
    public string TagName { get; } = "PrintableString";
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }

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
}
