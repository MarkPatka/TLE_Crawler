using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.Common;
using TLECrawler.Application.DAL;
using TLECrawler.Domain.Common.Configurations;

namespace TLECrawler.Infrastructure.DAL;

public class TLEDBFactory : ITLEDBFactory
{
    private readonly IOptions<DataBaseSettings> _databaseOptions;
    private readonly ILogger<TLEDBFactory> _logger;
    private readonly IDataProtector _protector;
    private readonly IMemoryCache _cache;    

    public TLEDBFactory(
        IOptions<DataBaseSettings> databaseOptions, 
        IDataProtectionProvider protectionProvider, 
        IMemoryCache cache,
        ILogger<TLEDBFactory> logger)
    {
        _databaseOptions = databaseOptions;
        
        _protector = protectionProvider
            .CreateProtector("ConnectionStrings");

        _cache = cache;
        _logger = logger;
    }

    public DataBaseSettings GetDatabaseCredentials()
    {
        string source = _protector
            .Protect(_databaseOptions.Value.DataSource);

        string catalog = _protector
            .Protect(_databaseOptions.Value.InitialCatalog);

        string user = _protector
            .Protect(_databaseOptions.Value.UserID);

        string pass = _protector
            .Protect(_databaseOptions.Value.Password);

        return new DataBaseSettings() 
        { 
            DataSource = source, 
            InitialCatalog = catalog, 
            UserID = user,
            Password = pass
        };
    }

    public SqlConnection InitializeConnection()
    {
        var options = _databaseOptions.Value;

        string Source = _protector.Unprotect(options.DataSource);
        string Catalog = _protector.Unprotect(options.InitialCatalog);
        string User = _protector.Unprotect(options.UserID);
        string Password = _protector.Unprotect(options.Password);

        string cs =
            $"Data Source={Source};" +
            $"Initial Catalog={Catalog};" +
            $"User ID={User};" +
            $"Password={Password};" +
            $"Connect Timeout={options.Timeout};" +
            $"Encrypt=True;" +
            $"Pooling=True;" +
            $"Trust Server Certificate=True;";

        return new SqlConnection(cs);
    }
    public SqlCommand CreateSqlCommand(SqlConnection connection, string sqlCommand = "", int timeout = 60)
    {
        if (connection == null)
            throw new InvalidOperationException("Sql connections is not initialized");
        
        if (string.IsNullOrEmpty(sqlCommand))
        {
            return connection.CreateCommand();
        }

        var command = connection.CreateCommand();
        command.CommandText = sqlCommand;
        command.CommandTimeout = timeout;
        return command;
    }
    public SqlCommand CreateSqlCommand(SqlConnection connection, SqlTransaction transaction, string sqlCommand = "", int timeout = 60)
    {
        if (connection == null)
            throw new InvalidOperationException("Sql connections is not initialized");

        if (string.IsNullOrEmpty(sqlCommand))
        {
            return connection.CreateCommand();
        }

        var command = connection.CreateCommand();
        command.CommandText = sqlCommand;
        command.Transaction = transaction;
        command.CommandTimeout = timeout;
        return command;
    }
    public SqlParameter CreateSqlParameter(string name, SqlDbType type, object value, ParameterDirection? direction = null)
    {
        return value == null 
            ? new SqlParameter(name, type) { Value = DBNull.Value } 
            : new SqlParameter(name, type) { Value = value };
    }

    public void ExecuteTransaction(SqlConnection connection, string sqlCommand)
    {
        SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            SqlCommand command_1 = CreateSqlCommand(connection, sqlCommand);
            command_1.Transaction = transaction;
            command_1.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception("Transaction rollback. For more details see inner exception: ", ex);
        }
    }
    public void ExecuteStoredProcedure(
        SqlConnection connection, 
        string procedureName, 
        SqlParameter[] sqlParameters,
        SqlTransaction? transaction = null)
    {
        SqlCommand command = new(procedureName);
        try
        {
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            command.CommandType = CommandType.StoredProcedure;
            command.Connection = connection;
            command.Parameters.AddRange(sqlParameters);
            command.ExecuteNonQuery();
        }
        catch (Exception ex) 
        {
            throw new Exception(ex.Message, ex.InnerException);
        }
    }
    public async Task<SqlDataReader> ExecuteStoredProcedureAsync(
        SqlConnection connection, 
        string procedureName, 
        SqlParameter[] sqlParameters, 
        SqlTransaction? transaction = null)
    {
        SqlCommand command = new(procedureName) 
        {
            Transaction = transaction ?? null,
            CommandType = CommandType.StoredProcedure,
            Connection = connection,
        };
        command.Parameters.AddRange(sqlParameters);        
        
        try
        {
            var reader = await command.ExecuteReaderAsync();
            return reader;
        }
        catch (DbException ex) 
        {
            throw new Exception($"Procedure ({procedureName}) execution failed.", ex);
        }        
    }

    private async Task<SqlConnection> SetConnectionAsync(string cs)
    {
        var sqlConnection = new SqlConnection(cs);
        try
        {
            await sqlConnection.OpenAsync();
        }
        catch (Exception exception)
        {
            string msg = "An error occurred while connecting to the database";            
            _logger.LogError(exception, "{MSG}", msg);
            throw new Exception(msg, exception);
        }
        return sqlConnection;
    }
    private SqlConnection SetConnection(string cs)
    {
        var sqlConnection = new SqlConnection(cs);
        try
        {
            sqlConnection.Open();
        }
        catch (Exception exception)
        {
            string msg = "An error occurred while connecting to the database";
            _logger.LogError(exception, "{MSG}", msg);
            throw new Exception(msg, exception);
        }
        return sqlConnection;
    }
    private static bool SqlServerConnected(string connectionString)
    {
        using SqlConnection connection = new(connectionString);
        try
        {
            connection.Open();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
        finally
        {
            connection.Close();
        }
    }

    public async Task<int> ExecuteStoredProcedureAsNonQueryAsync(
        SqlConnection connection, string procedureName, SqlParameter[] sqlParameters, SqlTransaction? transaction = null)
    {
        using SqlCommand command = new(procedureName)
        {
            Transaction = transaction ?? null,
            CommandType = CommandType.StoredProcedure,
            Connection = connection,
        };
        command.Parameters.AddRange(sqlParameters);

        try
        {
            return await command.ExecuteNonQueryAsync();
        }
        catch (DbException ex)
        {
            throw new Exception($"Procedure ({procedureName}) execution failed.", ex);
        }
    }
}
