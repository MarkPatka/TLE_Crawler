using System.Diagnostics.CodeAnalysis;

namespace TLECrawler.Domain.TLEModel;

public class TLEEqualityComparer : IEqualityComparer<TLE_ST>
{
    public bool Equals(TLE_ST? x, TLE_ST? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        bool rowsAreEqual =
            string.Equals(x.FirstRow, y.FirstRow) &&
            string.Equals(x.FirstRow, y.SecondRow);         

        bool publishDatesAreEqual =
            x.PublishDate == y.PublishDate;

        return rowsAreEqual && publishDatesAreEqual;
    }

    public int GetHashCode([DisallowNull] TLE_ST obj)
    {
        var value =
            ~obj.GetHashCode() +
            (obj.FirstRow.GetHashCode() << 8) +
            (obj.SecondRow.GetHashCode() << 16) +
            (obj.PublishDate.GetHashCode() << 24);

        value ^= (value >> 24);
        value = (value + (value << 3)) + (value << 8);
        value ^= (value >> 14);
        value = (value + (value << 5)) + (value << 11);
        value ^= (value >> 28);

        return value;
    }
}
