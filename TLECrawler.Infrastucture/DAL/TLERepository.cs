using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Data;

using TLECrawler.Application.DAL;
using TLECrawler.Domain.TLEModel;
using TLECrawler.Helpers.SqlHelper;
using TLECrawler.Helpers.TypeHelper;

namespace TLECrawler.Infrastructure.DAL;

public class TLERepository : ITLERepository
{
    private readonly ITLEDBFactory _tleDataBase;
    private readonly ILogger<TLERepository> _logger;

    public TLERepository(ITLEDBFactory tleDataBase, ILogger<TLERepository> logger) => 
        (_tleDataBase, _logger) = (tleDataBase, logger);

    public TLE Get(byte[] HashCode, int year)
    {
        string command = TLESQL.GetByHashFromPartition(HashCode, year);
        
        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);
        using SqlDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SingleResult);

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
    public async Task<List<TLE>> GetByHashes(IEnumerable<byte[]> hashCodes)
    {
        var HashCodes = hashCodes.ToList();
        string query = TLESQL.GetBatchByHash(hashCodes.Count());
        List<TLE> tles = [];

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        SqlCommand command = _tleDataBase.CreateSqlCommand(connection, transaction, query, 600);

        for (int i = 0; i < HashCodes.Count; i++)
        {
            command.Parameters.Add($"@p{i + 1}", SqlDbType.Binary).Value = HashCodes[i];
        }
        using SqlDataReader reader = await command.ExecuteReaderAsync();

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
        return tles;
    }
    public TLE Get(int id)
    {
        string command = TLESQL.GetById(id);

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);
        using SqlDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SingleResult);

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
    public DateTime GetDateTimeOfLastUploadedTLE()
    {
        string command = TLESQL.GetLastTLEUploadDate();

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);
        using SqlDataReader reader = sqlCommand.ExecuteReader();
        DateTime? startDate = null;
        try
        {
            while (reader.Read()) 
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
    public List<TLE> Get(IEnumerable<byte[]> hashCodes, int year)
    {
        var HashCodes = hashCodes.ToList();

        string query = TLESQL.GetBatchFromPartitionByHash(HashCodes.Count, year);
        List<TLE> tles = [];

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlCommand command = _tleDataBase.CreateSqlCommand(connection, query, 600);

        for (int i = 0; i < HashCodes.Count; i++)
        {
            command.Parameters.Add($"@p{i + 1}", SqlDbType.Binary).Value = HashCodes[i];
        }
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
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
    public List<TLE> GetFromPartition(int partitionYear)
    {
        string command = TLESQL.GetAllFromPartition(partitionYear);
        
        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);        
        using SqlDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SequentialAccess);
        
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
    public void InsertOne(TLE tle)
    {
        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlTransaction transaction = connection.BeginTransaction();
        
        try
        {
            _tleDataBase.ExecuteStoredProcedure(connection, "writeTLE",
            [
                _tleDataBase.CreateSqlParameter("@firstRow",    SqlDbType.VarChar,  tle.FirstRow),
                _tleDataBase.CreateSqlParameter("@secondRow",   SqlDbType.VarChar,  tle.SecondRow),
                _tleDataBase.CreateSqlParameter("@publishDate", SqlDbType.DateTime, tle.PublishDate),
                _tleDataBase.CreateSqlParameter("@hash",        SqlDbType.Binary,   tle.Hash),
                _tleDataBase.CreateSqlParameter("@iterationId", SqlDbType.Int,      tle.IterationId)
            ], 
            transaction);
            transaction.Commit();

            _logger.LogInformation("New TLE added successfully");
        }
        catch (Exception ex) 
        {
            transaction.Rollback();

            string msg = 
                $"Procedure (writeTLE) execution failed. " +
                $"Procedure transaction rollback";

            _logger.LogError(ex, "{MSG}", msg);

            throw new Exception(msg, ex);
        }        
    }
    public void InsertMany(IEnumerable<TLE> tles)
    {
        DataTable TLEDataTable = CreateInMemoryTleDataTable([.. tles]);

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        {
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            using SqlBulkCopy sqlBulkCopy = new(connection, SqlBulkCopyOptions.CheckConstraints, transaction);
            try
            {
                sqlBulkCopy.DestinationTableName = "dbo.TLEs";

                sqlBulkCopy.ColumnMappings.Add("FirstRow", "FirstRow");
                sqlBulkCopy.ColumnMappings.Add("SecondRow", "SecondRow");
                sqlBulkCopy.ColumnMappings.Add("PublishDate", "PublishDate");
                sqlBulkCopy.ColumnMappings.Add("Hash", "Hash");
                sqlBulkCopy.ColumnMappings.Add("IterationId", "IterationId");

                sqlBulkCopy.WriteToServer(TLEDataTable);

                int cnt = tles.Count();
                _logger.LogInformation("{CNT} new TLEs were added successfully", cnt);
            }
            catch (InvalidOperationException ex)
            {
                string msg =
                    "An error occured during SqlBulkCopy operation. " +
                    $"Source: {nameof(InsertMany)}.";

                _logger.LogError(ex, "{MSG}", msg);

                throw new Exception(msg ,ex);
            }            
        }        
    }
    public async Task InsertManyAsync(List<TLE> tles)
    {
        var duplicate = tles.FirstOrDefault(t => t.Hash.SimpleEqualityCheck(new byte[16]));
        DataTable dataTable = CreateInMemoryTleDataTable(tles);

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        {
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            using SqlCommand command = new("dbo.InsertTLEs", connection, transaction);
            try
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 600;

                SqlParameter tvpParam = command.Parameters.AddWithValue("@TLEs", dataTable);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "dbo.TleTvpTableType";

                await command.ExecuteNonQueryAsync();
                transaction.Commit();

                int cnt = tles.Count;
                _logger.LogInformation("{CNT} new TLEs added", cnt);
            }
            catch (Exception ex) 
            {               
                string msgError =
                    "An error occured during the TLEs insertion. " +
                    $"Source: {nameof(InsertManyAsync)} procedure.";

                _logger.LogError(ex, "{MSG}", msgError);

                transaction.Rollback();

                string msgRollback = "Insert transaction rolled back.";
                _logger.LogError(ex, "{MSG}", msgRollback);

                throw new Exception(msgError, ex);
            }
        }
    }
    public async Task<List<TLE>> GetAsync(IEnumerable<byte[]> hashCodes, int year)
    {
        var HashCodes = hashCodes.ToList();

        string query = TLESQL.GetBatchFromPartitionByHash(HashCodes.Count, year);
        List<TLE> tles = [];

        using SqlConnection connection = _tleDataBase.InitializeConnection();
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
    public async Task<List<TLE>> FetchTLEsAsync(IEnumerable<byte[]> hashCodes, int offset, int batchSize, int year)
    {
        var data = new List<TLE>();
        var HashCodes = hashCodes.ToList();
        string query = TLESQL.FetchBatchFromPartitionByHash(offset, batchSize, HashCodes.Count, year);

        using SqlConnection connection = _tleDataBase.InitializeConnection();
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
    public List<TLE> FetchTLEs(IEnumerable<byte[]> hashCodes, int offset, int batchSize, int year)
    {
        var data = new List<TLE>();
        var HashCodes = hashCodes.ToList();
        string query = TLESQL.FetchBatchFromPartitionByHash(offset, batchSize, HashCodes.Count, year);

        using SqlConnection connection = _tleDataBase.InitializeConnection();
        SqlCommand command = _tleDataBase.CreateSqlCommand(connection, query, 600);

        for (int i = 0; i < HashCodes.Count; i++)
        {
            command.Parameters.Add($"@p{i + 1}", SqlDbType.Binary).Value = HashCodes[i];
        }

        using SqlDataReader reader =  command.ExecuteReader();

        while (reader.Read())
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