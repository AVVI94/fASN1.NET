using System.Diagnostics.CodeAnalysis;
using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// The BER/DER parser behind <see cref="Asn1Serializer.Deserialize(byte[])"/> and its overloads.
/// </summary>
/// <remarks>
/// Parsing runs in two passes over the input:
/// <list type="number">
/// <item><see cref="StructureScanner{TSource}"/> validates the structure and decides the tree's shape
/// without creating tags.</item>
/// <item><see cref="TreeBuilder"/> creates exactly the tags of the final tree and reads only their content.</item>
/// </list>
/// Nothing is allocated for structure that is later discarded, and invalid input allocates nothing but the
/// error message. Working buffers are rented from the shared array pools for one call; the parser keeps no
/// state between calls.
/// </remarks>
internal static class Asn1Parser
{
    /// <summary>
    /// Parses the element at the source's current position. On success the source is left just past it.
    /// </summary>
    public static bool TryParse<TSource>(ref TSource source, ITagFactory tagFactory, [NotNullWhen(true)] out ITag? tag, [NotNullWhen(false)] out string? error)
        where TSource : struct, IByteSource
    {
        var scanner = new StructureScanner<TSource>(source);
        try
        {
            if (!scanner.TryScan())
            {
                tag = null;
                error = scanner.Failure.ToMessage(tagFactory);
                return false;
            }

            var end = scanner.Position;
            var failure = default(ParseFailure);
            if (!TreeBuilder.TryBuild(ref source, scanner.Nodes, scanner.Segments, tagFactory, out tag, ref failure))
            {
                tag = null;
                error = failure.ToMessage(tagFactory);
                return false;
            }

            source.Position = end;
            error = null;
            return true;
        }
        finally
        {
            scanner.Dispose();
        }
    }
}
