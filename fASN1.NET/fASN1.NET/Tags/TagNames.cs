using System;

namespace fASN1.NET.Tags;

/// <summary>
/// Cached names for tags whose name depends on the tag number, so that creating such a tag does not
/// allocate a string.
/// </summary>
internal static class TagNames
{
    private static readonly string[] Context = Build(n => $"[{n}]");
    private static readonly string[] ApplicationNames = Build(n => $"Application_{n}");
    private static readonly string[] PrivateNames = Build(n => $"Private_{n}");
    private static readonly string[] UnknownNames = Build(n => $"Unknown_{n}");

    private static string[] Build(Func<int, string> name)
    {
        var names = new string[32];
        for (int i = 0; i < names.Length; i++)
            names[i] = name(i);
        return names;
    }

    public static string ContextSpecific(int tagNumber) => Context[tagNumber & 0x1F];
    public static string Application(int tagNumber) => ApplicationNames[tagNumber & 0x1F];
    public static string Private(int tagNumber) => PrivateNames[tagNumber & 0x1F];
    public static string Unknown(int tagNumber) => UnknownNames[tagNumber & 0x1F];
}
