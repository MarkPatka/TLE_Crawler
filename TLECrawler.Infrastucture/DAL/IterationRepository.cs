using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using TLECrawler.Application.DAL;
using TLECrawler.Domain.Common;
using TLECrawler.Domain.IterationModel;
using TLECrawler.Helpers.SqlHelper;

namespace TLECrawler.Infrastructure.DAL;

public class IterationRepository : IIterationRepository
{
    private readonly ITLEDBFactory _tleDataBase;
    private readonly ILogger<IterationRepository> _logger;

    public IterationRepository(ITLEDBFactory tleDataBase, ILogger<IterationRepository> logger) =>
        (_tleDataBase, _logger) = (tleDataBase, logger);

    /// <summary>
    /// <br>Executes a stored procedure which creates an Iteration initializing the following fields:</br>
    /// <br>[ bool IsRepeat ]</br>
    /// </summary>
    /// <returns>ID of the iteraton which is initialized at the Db layer</returns>
    public int InitializeIteration()
    {
        var outputId = _tleDataBase.CreateSqlParameter(
            "@output_id", SqlDbType.Int, -1, ParameterDirection.Output);

        var isRepeat = _tleDataBase.CreateSqlParameter(
            "@isRepeat",  SqlDbType.Bit, false);

        var outputStart = _tleDataBase.CreateSqlParameter(
            "@output_startDateTime", SqlDbType.DateTime, DateTime.UtcNow, ParameterDirection.Output);

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        try
        {
            _tleDataBase.ExecuteStoredProcedure(connection, "saveIteration", [outputId, isRepeat, outputStart]);

            return Convert.ToInt32(outputId.Value);
        }
        catch (Exception ex) 
        {
            throw new Exception(
                "An error occured while executing stored procedure named \"saveIteration\". For more details see: ", ex);
        }
    }
    /// <summary>
    /// Executes the stored procedure "saveIteration",
    /// which persist new iteration with only (id, startDateTime, isRepeat) fields filled
    /// </summary>
    /// <returns>The id of created iteration</returns>
    public async Task<int> InitializeIterationAsync()
    {
        var isRepeat = _tleDataBase.CreateSqlParameter(
            "@isRepeat", SqlDbType.Bit, false);

        var dt = _tleDataBase.CreateSqlParameter(
            "@startDateTime", SqlDbType.DateTime, DateTime.UtcNow);

        int resultId = -1;
        using SqlConnection connection = _tleDataBase.InitializeConnection();
        try
        {
            var command = _tleDataBase.CreateSqlCommand(connection, IterationsSQL.InsertPreinitializedIteration());
            command.Parameters.Add(dt);
            command.Parameters.Add(isRepeat);
            SqlDataReader reader = await command.ExecuteReaderAsync();
            try
            {
                while (reader.Read()) 
                {
                    resultId = reader.GetInt32(0);
                }
            }
            finally
            {
                reader.Close();
            }
            _logger.LogInformation("New Iteration initialized. Id: {ID}", resultId);
            return resultId;
        }
        catch (DbException ex)
        {
            string message = 
                "An error occured while executing stored procedure named \"saveIteration\". " +
               $"Source: {nameof(InitializeIterationAsync)}";

            _logger.LogError(ex, "{MESSAGE}", message);

            throw new CreateIterationExcepion(message, -1, ex);
        }
    }
    public Iteration? GetById(int id)
    {
        string command = IterationsSQL.GetById(id);
        Iteration? result = null;
        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command);
        using SqlDataReader reader = sqlCommand.ExecuteReader();

        if (reader.Read())
        {
            DateTime start = reader.GetDateTime(1);

            IterationStatus? status = (reader[2] is byte)
                ? Enumeration.GetFromId<IterationStatus>(reader.GetByte(2))
                : null;

            int? count = (reader[3] is int?)
                ? reader.GetInt32(3)
                : null;

            DateTime? end = (reader[4] is DateTime?)
                ? reader.GetDateTime(4)
                : null;

            bool? repeat = (reader[5] is bool?)
                ? reader.GetBoolean(5)
                : null;

            result = new(start, status, count, end, repeat);
        }
        return result;
    }
    public Iteration GetLast()
    {
        string command = IterationsSQL.GetLast();

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command);
        using SqlDataReader reader = sqlCommand.ExecuteReader();

        try
        {
            Iteration? result = null;
            if (reader.Read())
            {
                DateTime start = reader.GetDateTime(1);

                IterationStatus? status = (reader[2] is byte)
                    ? Enumeration.GetFromId<IterationStatus>(reader.GetByte(2)) 
                    : null;

                int? count = (reader[3] is int?) 
                    ? reader.GetInt32(3) 
                    : null;

                DateTime? end = (reader[4] is DateTime?) 
                    ? reader.GetDateTime(4) 
                    : null;

                bool? repeat = (reader[5] is bool?) 
                    ? reader.GetBoolean(5) 
                    : null;

                result = new(start, status, count, end, repeat);
            }
            return result!;

        }
        catch (Exception ex) 
        {
            string message = 
                "An error occurred while trying to get last iteration from Db";

            _logger.LogError(ex, "{MESSAGE}", message);
            throw new Exception(ex.Message, ex.InnerException);
        }
    }
    public void CompleteIteration(int id, Iteration iteration)
    {
        using SqlConnection connection = _tleDataBase.InitializeConnection();

        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(
            connection, IterationsSQL.Update(id));

        SqlParameter[] parameters = 
        [
            _tleDataBase.CreateSqlParameter("@StartDateTime" ,SqlDbType.DateTime, iteration.StartDateTime),
            _tleDataBase.CreateSqlParameter("@EndDateTime" ,SqlDbType.DateTime, iteration.EndDateTime ?? DateTime.UtcNow),
            _tleDataBase.CreateSqlParameter("@statusID" ,SqlDbType.TinyInt, iteration.Status?.ID ?? IterationStatus.UNKNOWN.ID),
            _tleDataBase.CreateSqlParameter("@TLECount" ,SqlDbType.Int ,iteration.TLECount ?? -1),
            _tleDataBase.CreateSqlParameter("@IsRepeat" ,SqlDbType.Bit ,false )
        ];
        sqlCommand.Parameters.AddRange(parameters);     
        try
        {
            sqlCommand.ExecuteNonQuery();
            
            _logger.LogInformation("Iteration {ID} written", id);
        }
        catch (Exception ex)
        {
            string msg = 
               $"Iteration {id} was not written successfully. " +
                "The error occurred while updating the iteration info. ";

            _logger.LogError(ex, "{MSG}", msg);
        }
    }
}

