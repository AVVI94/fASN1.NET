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
        {
            names[i] = name(i);
        }

        return names;
    }

    public static string ContextSpecific(int tagNumber) => Name(_contextNames, tagNumber, "[{0}]");
    public static string Application(int tagNumber) => Name(_applicationNames, tagNumber, "Application_{0}");
    public static string Private(int tagNumber) => Name(_privateNames, tagNumber, "Private_{0}");
    public static string Unknown(int tagNumber) => Name(_unknownNames, tagNumber, "Unknown_{0}");

    /// <summary>The cached name of a single-byte tag number, or a new one for a larger number.</summary>
    private static string Name(string[] cache, int tagNumber, string format)
        => tagNumber < cache.Length ? cache[tagNumber] : string.Format(System.Globalization.CultureInfo.InvariantCulture, format, tagNumber);
}
