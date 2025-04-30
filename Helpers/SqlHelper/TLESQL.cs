namespace TLECrawler.Helpers.SqlHelper;

public static class TLESQL
{
    /// <summary>
    /// Find and return one tle 
    /// </summary>
    /// <param name="hash">unique hash</param>
    /// <param name="year">partition year</param>
    /// <returns>One tle object if found</returns>
    public static string GetByHashFromPartition(byte[] hash, int year) =>
        "SELECT TOP(1) * " +
        "FROM TLEs " +
       $"WHERE PartitionYear = {year} AND Hash = {hash}";

    /// <summary>
    /// Find and return one tle 
    /// </summary>
    /// <param name="id">primary key</param>
    /// <returns>One tle object if found</returns>
    public static string GetById(int id) =>
        $"SELECT TOP(1) * " +
        $"FROM TLEs WHERE Id = {id}";

    public static string GetBatchFromPartitionByHash(int batchSize, int partitionYear)
    {
        string query =
            $"SELECT TOP({batchSize}) * FROM TLEs " +
            $"WHERE PartitionYear IN ({partitionYear}, {partitionYear - 1})  AND Hash IN (";

        for (int i = 1; i < batchSize; i++)
            query += $"@p{i}, ";

        query += $"@p{batchSize})";
        return query;
    }
    public static string GetBatchByHash(int batchSize)
    {
        string query =
            $"SELECT TOP({batchSize}) * FROM TLEs WHERE Hash IN (";

        for (int i = 1; i < batchSize; i++)
            query += $"@p{i}, ";

        query += $"@p{batchSize})";
        return query;
    }
    public static string FetchBatchFromPartitionByHash(int offset, int batchSize, int hashCodesCount, int partitionYear)
    {
        string query =
            $"SELECT * FROM TLEs " +
            $"WHERE PartitionYear = {partitionYear} AND Hash IN (";

        for (int i = 1; i < hashCodesCount; i++)
            query += $"@p{i}, ";

        query += $"@p{hashCodesCount}) ";

        query += $"ORDER BY PublishDate OFFSET {offset} ROWS FETCH NEXT {batchSize} ROWS ONLY";
        return query;
    }
    public static string GetLastTLEUploadDate() =>
        $"SELECT TOP(1) PublishDate " +
        $"FROM TLEs " +
        $"ORDER BY PublishDate " +
        $"DESC";

    public static string GetAllFromPartition(int partitionYear) =>
        $"SELECT * " +
        $"FROM TLEs " +
        $"WHERE PartitionYear = {partitionYear}";

    public static string UpdateHashCode(int partitionYear) =>
        $"UPDATE TLEs " +
        $"SET Hash = @NewHash " +
        $"WHERE PartitionYear = {partitionYear} AND Hash = @OldHash";
}
