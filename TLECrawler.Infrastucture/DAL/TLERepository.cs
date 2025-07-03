using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Data;
using System.Data.Common;
using TLECrawler.Application.DAL;
using TLECrawler.Domain.TLEModel;
using TLECrawler.Helpers.SqlHelper;

namespace TLECrawler.Infrastructure.DAL;

public class TLERepository : ITLERepository
{
    private readonly ITLEDBFactory _tleDataBase;
    private readonly ILogger<TLERepository> _logger;

    public TLERepository(ITLEDBFactory tleDataBase, ILogger<TLERepository> logger) => 
        (_tleDataBase, _logger) = (tleDataBase, logger);

    public async Task<TLE> GetAsync(byte[] HashCode, int year)
    {
        string command = TLESQL.GetByHashFromPartition(HashCode, year);
        
        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);
        using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SingleResult);

        byte[] hash = new byte[16];
        var bytesRead = reader.GetBytes(5, 0, hash, 0, 16);

        TLE tle = new(
            PublishDate: reader.GetDateTime(3),
            FirstRow: reader.GetString(1),
            SecondRow: reader.GetString(2),
            Hash: hash,
            IterationId: reader.GetInt32(4));

        return tle;
    }
    public async Task<TLE> GetAsync(int id)
    {
        string command = TLESQL.GetById(id);

        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);
        using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SingleResult);

        byte[] hash = new byte[16];
        var bytesRead = reader.GetBytes(5, 0, hash, 0, 16);

        TLE tle = new(
            PublishDate: reader.GetDateTime(3),
            FirstRow: reader.GetString(1),
            SecondRow: reader.GetString(2),
            Hash: hash,
            IterationId: reader.GetInt32(4));

        return tle;
    }
    public async Task<List<TLE>> GetAsync(IEnumerable<byte[]> hashCodes, int year)
    {
        var HashCodes = hashCodes.ToList();

        string query = TLESQL.GetBatchFromPartitionByHash(HashCodes.Count, year);
        List<TLE> tles = [];

        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        SqlCommand command = _tleDataBase.CreateSqlCommand(connection, query, 600);

        for (int i = 0; i < HashCodes.Count; i++)
        {
            command.Parameters.Add($"@p{i + 1}", SqlDbType.Binary).Value = HashCodes[i];
        }
        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            byte[] hash = new byte[16];
            var bytesRead = reader.GetBytes(5, 0, hash, 0, 16);

            TLE tle = new(
                PublishDate: reader.GetDateTime(3),
                FirstRow: reader.GetString(1),
                SecondRow: reader.GetString(2),
                Hash: hash,
                IterationId: reader.GetInt32(4));

            tles.Add(tle);
        }
        return tles;
    }
    public async Task<List<TLE>> GetByHashesAsync(IEnumerable<byte[]> hashCodes)
    {
        var HashCodes = hashCodes.ToList();
        string query = TLESQL.GetBatchByHash(hashCodes.Count());
        List<TLE> tles = new(HashCodes.Count);

        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        using SqlCommand command = _tleDataBase.CreateSqlCommand(connection, transaction, query, 600);

        for (int i = 0; i < HashCodes.Count; i++)
        {
            command.Parameters.Add($"@p{i + 1}", SqlDbType.Binary).Value = HashCodes[i];
        }

        using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection))
        {
            while (await reader.ReadAsync())
            {
                byte[] hash = new byte[16];
                _ = reader.GetBytes(5, 0, hash, 0, 16);

                TLE tle = new(
                    PublishDate: reader.GetDateTime(3),
                    FirstRow: reader.GetString(1),
                    SecondRow: reader.GetString(2),
                    Hash: hash,
                    IterationId: reader.GetInt32(4));

                tles.Add(tle);
            }
        }

        await transaction.CommitAsync();
        return tles;
    }
    public async Task<List<TLE>> GetByTvpHashesAsync(IEnumerable<byte[]> hashCodes)
    {
        //1. Create TVP
        var hashTable = new DataTable();
        hashTable.Columns.Add("Hash", typeof(byte[]));

        foreach (var hash in hashCodes)
            hashTable.Rows.Add(hash);

        // 2. Set sql command
        await using var connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        // 3. Execute and map results
        var tles = new List<TLE>(hashTable.Rows.Count);
        try
        {
            SqlCommand command = new("GetTLEsByHashes", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure,
            };
            SqlParameter tvp = new("@Hashes", SqlDbType.Structured)
            {
                TypeName = "HashTableType",
                Value = hashTable
            };
            command.Parameters.Add(tvp);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    tles.Add(new TLE(
                        PublishDate: reader.GetDateTime("PublishDate"),
                        FirstRow: reader.GetString("FirstRow"),
                        SecondRow: reader.GetString("SecondRow"),
                        Hash: (byte[])reader["Hash"],
                        IterationId: reader.GetInt32("IterationId"))
                    );
                }
            }

            await transaction.CommitAsync();

            return tles;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            string msg =
                $"Execution of the stored procedure \"GetTLEsByHashes\" failed. " +
                $"Transaction rollback";

            _logger.LogError(ex, "{MSG}", msg);
            throw new Exception(msg, ex);
        }

    }
    public async Task<DateTime> GetDateTimeOfLastUploadedTLEAsync()
    {
        string command = TLESQL.GetLastTLEUploadDate();

        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);
        using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
        DateTime? startDate = null;
        try
        {
            while (await reader.ReadAsync()) 
            {
                startDate = reader.GetDateTime(0);
                var t = reader[0];
                break;
            }            
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "{MSG}", ex.Message);
            throw;
        }    
        return startDate ?? throw new NullReferenceException(nameof(startDate));
    }
    public async  Task<List<TLE>> GetFromPartitionAsync(int partitionYear)
    {
        string command = TLESQL.GetAllFromPartition(partitionYear);
        
        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);        
        using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        
        byte[] hash = new byte[16];
        List<TLE> tles = [];

        while (reader.Read())
        {
            var bytesRead = reader.GetBytes(5, 0, hash, 0, 16);

            TLE tle = new(
                PublishDate: reader.GetDateTime(3),
                FirstRow:    reader.GetString(1),
                SecondRow:   reader.GetString(2),
                Hash:        hash,
                IterationId: reader.GetInt32(4));

            tles.Add(tle);
        }
        return tles;
    }
    
    public async Task InsertOneAsync(TLE tle)
    {
        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        await using DbTransaction dbTransaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var transaction = (SqlTransaction)dbTransaction;
        try
        {

            await _tleDataBase.ExecuteStoredProcedureAsNonQueryAsync(connection, "writeTLE",
            [
                _tleDataBase.CreateSqlParameter("@firstRow",    SqlDbType.VarChar,  tle.FirstRow),
                _tleDataBase.CreateSqlParameter("@secondRow",   SqlDbType.VarChar,  tle.SecondRow),
                _tleDataBase.CreateSqlParameter("@publishDate", SqlDbType.DateTime, tle.PublishDate),
                _tleDataBase.CreateSqlParameter("@hash",        SqlDbType.Binary,   tle.Hash),
                _tleDataBase.CreateSqlParameter("@iterationId", SqlDbType.Int,      tle.IterationId)
            ],
            transaction);

            await transaction.CommitAsync();
            _logger.LogInformation("New TLE added successfully");
        }
        catch (Exception ex) 
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction");
            }

            string msg = $"Procedure (writeTLE) execution failed. Procedure transaction rollback";
            _logger.LogError(ex, "{MSG}", msg);
            throw new Exception(msg, ex);
        }        
    }
    public async Task InsertManyAsync(IEnumerable<TLE> tles)
    {
        DataTable TLEDataTable = CreateInMemoryTleDataTable([.. tles]);

        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            using (SqlBulkCopy sqlBulkCopy = new(connection, SqlBulkCopyOptions.CheckConstraints, transaction))
            {

                sqlBulkCopy.ColumnMappings.Add("FirstRow", "FirstRow");
                sqlBulkCopy.ColumnMappings.Add("SecondRow", "SecondRow");
                sqlBulkCopy.ColumnMappings.Add("PublishDate", "PublishDate");
                sqlBulkCopy.ColumnMappings.Add("Hash", "Hash");
                sqlBulkCopy.ColumnMappings.Add("IterationId", "IterationId");

                await sqlBulkCopy.WriteToServerAsync(TLEDataTable);
                sqlBulkCopy.DestinationTableName = "dbo.TLEs";
            }

            await transaction.CommitAsync();

            int cnt = tles.Count();
            _logger.LogInformation("{CNT} new TLEs were added successfully", cnt);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            string msg =
                "An error occured during SqlBulkCopy operation. " +
                $"Source: {nameof(InsertManyAsync)}.";

            _logger.LogError(ex, "{MSG}", msg);

            throw new Exception(msg ,ex);
        }            
    }
    public async Task InsertManyAsync(List<TLE> tles)
    {
        if (tles.Count == 0) return;

        DataTable dataTable = CreateInMemoryTleDataTable(tles);

        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            SqlCommand command = new("dbo.InsertTLEs", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 600
            };
            SqlParameter tvp = new("@TLEs", SqlDbType.Structured)
            {
                TypeName = "TleTvpTableType",
                Value = dataTable
            };
            command.Parameters.Add(tvp);

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            int cnt = tles.Count;
            _logger.LogInformation("{CNT} new TLEs added", cnt);
        }
        catch (Exception ex) 
        {
            await transaction.RollbackAsync();

            string msgError =
                "An error occured during the TLEs insertion. " +
                $"Source: {nameof(InsertManyAsync)} procedure. " +
                "Insert transaction rolled back.";

            _logger.LogError(ex, "{MSG}", msgError);
            throw new Exception(msgError, ex);
        }
    }

    public async Task<List<TLE>> FetchTLEsAsync(IEnumerable<byte[]> hashCodes, int offset, int batchSize, int year)
    {
        var data = new List<TLE>();
        var HashCodes = hashCodes.ToList();
        string query = TLESQL.FetchBatchFromPartitionByHash(offset, batchSize, HashCodes.Count, year);

        await using SqlConnection connection = _tleDataBase.InitializeConnection();
        await connection.OpenAsync();
        SqlCommand command = _tleDataBase.CreateSqlCommand(connection, query, 600);

        for (int i = 0; i < HashCodes.Count; i++)
        {
            command.Parameters.Add($"@p{i + 1}", SqlDbType.Binary).Value = HashCodes[i];
        }

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            byte[] hash = new byte[16];
            var bytesRead = reader.GetBytes(5, 0, hash, 0, 16);

            TLE tle = new(
                PublishDate: reader.GetDateTime(3),
                FirstRow: reader.GetString(1),
                SecondRow: reader.GetString(2),
                Hash: hash,
                IterationId: reader.GetInt32(4));

            data.Add(tle);
        }
        return data;
    }
    
    private static DataTable CreateInMemoryTleDataTable(List<TLE> tles)
    {
        DataTable TLEDataTable = new();
        TLEDataTable.Columns.Add("PublishDate", typeof(DateTime));
        TLEDataTable.Columns.Add("FirstRow", typeof(string));
        TLEDataTable.Columns.Add("SecondRow", typeof(string));
        TLEDataTable.Columns.Add("Hash", typeof(byte[]));
        TLEDataTable.Columns.Add("IterationId", typeof(int));

        foreach (var row in tles)
        {
            TLEDataTable.Rows.Add(
                row.PublishDate,
                row.FirstRow,
                row.SecondRow,
                row.Hash,
                row.IterationId);
        }
        TLEDataTable.AcceptChanges();

        return TLEDataTable;
    }
}