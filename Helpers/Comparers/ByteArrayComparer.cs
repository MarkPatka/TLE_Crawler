namespace TLECrawler.Helpers.Comparers;

public class ByteArrayComparer : IEqualityComparer<ReadOnlyMemory<byte>>
{
    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        => x.Span.SequenceEqual(y.Span);

    public int GetHashCode(ReadOnlyMemory<byte> obj)
    {
        var hash = new HashCode();

        foreach (var b in obj.Span)
            hash.Add(b);

        return hash.ToHashCode();
    }
}