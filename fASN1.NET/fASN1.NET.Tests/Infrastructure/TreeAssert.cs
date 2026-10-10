using System.Text;
using fASN1.NET.Tags;
using Xunit.Sdk;

namespace fASN1.NET.Tests.Infrastructure;

/// <summary>
/// Deep, property-by-property comparison of two <see cref="ITag"/> trees.
/// Walks with an explicit stack so arbitrarily deep trees can be compared.
/// Failure messages carry the path of the first difference, e.g. <c>root/0/3/1</c>.
/// </summary>
internal static class TreeAssert
{
    public static void Equal(ITag expected, ITag actual)
    {
        var stack = new Stack<(ITag Expected, ITag Actual, string Path)>();
        stack.Push((expected, actual, "root"));
        while (stack.Count > 0)
        {
            var (e, a, path) = stack.Pop();
            Check(path, "type", e.GetType().FullName, a.GetType().FullName);
            Check(path, nameof(ITag.TagNumber), e.TagNumber, a.TagNumber);
            Check(path, nameof(ITag.TagName), e.TagName, a.TagName);
            Check(path, nameof(ITag.TagClass), e.TagClass, a.TagClass);
            Check(path, nameof(ITag.IsConstructed), e.IsConstructed, a.IsConstructed);
            Check(path, nameof(ITag.IsUniversal), e.IsUniversal, a.IsUniversal);
            Check(path, nameof(ITag.IsEoc), e.IsEoc, a.IsEoc);
            if (!e.Content.AsSpan().SequenceEqual(a.Content))
            {
                throw Fail(path, nameof(ITag.Content), Convert.ToHexString(e.Content), Convert.ToHexString(a.Content));
            }
            Check(path, "Children.Count", e.Children.Count, a.Children.Count);
            for (int i = e.Children.Count - 1; i >= 0; i--)
            {
                stack.Push((e.Children[i], a.Children[i], $"{path}/{i}"));
            }
        }
    }

    /// <summary>
    /// Asserts the invariants every parsed tree must satisfy regardless of input.
    /// </summary>
    public static void WellFormed(ITag root)
    {
        foreach (var (tag, path) in Walk(root))
        {
            if (tag.Children.Count > 0 && tag.Content.Length != 0)
                throw new XunitException($"{path}: node has both children and content");
            if (tag.Children.Any(c => c is null))
                throw new XunitException($"{path}: null child");
        }
    }

    public static IEnumerable<(ITag Tag, string Path)> Walk(ITag root)
    {
        var stack = new Stack<(ITag, string)>();
        stack.Push((root, "root"));
        while (stack.Count > 0)
        {
            var (t, p) = stack.Pop();
            yield return (t, p);
            for (int i = t.Children.Count - 1; i >= 0; i--)
                stack.Push((t.Children[i], $"{p}/{i}"));
        }
    }

    public static int CountNodes(ITag root) => Walk(root).Count();

    /// <summary>
    /// Canonical one-line-per-node dump, used to make failures readable.
    /// </summary>
    public static string Dump(ITag root)
    {
        var sb = new StringBuilder();
        foreach (var (t, p) in Walk(root))
        {
            var depth = p.Count(c => c == '/');
            sb.Append(' ', depth * 2)
              .Append(t.GetType().Name).Append(" #").Append(t.TagNumber)
              .Append(" '").Append(t.TagName).Append("' cls=").Append(t.TagClass)
              .Append(t.IsConstructed ? " C" : " P")
              .Append(t.Children.Count > 0 ? $" children={t.Children.Count}" : $" content={Convert.ToHexString(t.Content)}")
              .AppendLine();
        }
        return sb.ToString();
    }

    private static void Check<T>(string path, string property, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw Fail(path, property, expected, actual);
    }

    private static XunitException Fail(string path, string property, object? expected, object? actual)
        => new($"Trees differ at {path}, {property}:{Environment.NewLine}  expected: {expected}{Environment.NewLine}  actual:   {actual}");
}
