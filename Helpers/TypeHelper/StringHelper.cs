using System.Buffers;

namespace TLECrawler.Helpers.TypeHelper;

public static class StringHelper
{
    public static string RemoveWhitespaces(this string source)
    {
        const int maxStackArray = 256; 

        if (source.Length < maxStackArray)
            return RemoveWhitespacesSpanHelper(source, stackalloc char[source.Length]);

        var pooledArray = ArrayPool<char>.Shared.Rent(source.Length);
        try
        {
            return RemoveWhitespacesSpanHelper(source, pooledArray.AsSpan(0, source.Length));
        }
        finally
        {
            ArrayPool<char>.Shared.Return(pooledArray);
        }
    }

    private static string RemoveWhitespacesSpanHelper(string source, Span<char> dest)
    {
        var pos = 0;

        foreach (var c in source)
            if (!char.IsWhiteSpace(c))
                dest[pos++] = c;

        return source.Length == pos ? source : new string(dest[..pos]);
    }
}
