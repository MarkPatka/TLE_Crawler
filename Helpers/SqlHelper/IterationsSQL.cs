namespace TLECrawler.Helpers.SqlHelper;

public static class IterationsSQL
{
    public static string GetById(int id) =>
        $"SELECT * FROM Iterations WHERE Id = {id}";

    public static string InsertPreinitializedIteration() =>
        "DECLARE @outputId INT; " +
        "INSERT INTO Iterations (StartDateTime, IsRepeat) " +
        "VALUES (@startDateTime, @isRepeat); " +
        "SET @outputId = SCOPE_IDENTITY() " +
        "SELECT @outputId AS NewIterationId;";

    public static string Update(int id) =>
       "SET NOCOUNT ON; " +
       "UPDATE Iterations " +
       "SET " +
       $"StartDateTime = @StartDateTime, " +
       $"EndDateTime = @EndDateTime, " +
       $"Status = @statusID, " +
       $"TleCount = @TLECount, " +
       $"IsRepeat = @IsRepeat " +
       $"WHERE Id = {id};";

    public static string GetLast() =>
        $"SELECT TOP(1) * " +
        $"FROM Iterations " +
        $"ORDER BY Id " +
        $"DESC";


}
