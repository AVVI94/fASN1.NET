using System;

namespace fASN1.NET.Tags;

/// <summary>
/// Cached names for tags whose name depends on the tag number, so that creating such a tag does not
/// allocate a string.
/// </summary>
internal static class TagNames
{
    private static readonly string[] _contextNames = Build(n => $"[{n}]");
    private static readonly string[] _applicationNames = Build(n => $"Application_{n}");
    private static readonly string[] _privateNames = Build(n => $"Private_{n}");
    private static readonly string[] _unknownNames = Build(n => $"Unknown_{n}");

    private static string[] Build(Func<int, string> name)
    {
        var names = new string[32];
        for (int i = 0; i < names.Length; i++)
            names[i] = name(i);
        return names;
    }

    public static string ContextSpecific(int tagNumber) => _contextNames[tagNumber & 0x1F];
    public static string Application(int tagNumber) => _applicationNames[tagNumber & 0x1F];
    public static string Private(int tagNumber) => _privateNames[tagNumber & 0x1F];
    public static string Unknown(int tagNumber) => _unknownNames[tagNumber & 0x1F];
}
