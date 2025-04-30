namespace TLECrawler.Helpers.Converters;

public static class CollectionToStringConverter
{
    public static string Convert<T>(IEnumerable<T> input, string separator = ",", string openBracket = "", string closeBracket = "")
    {
        string query = string.Empty;
        T[] inputArray = input.ToArray();

        query += openBracket;
        for (int i = 0; i < inputArray.Length - 1; i++)
        {
            query += $"{inputArray[i]}{separator} ";
        }
        query += $"{inputArray[^1]}{closeBracket}";

        return query;
    }
}
