using Microsoft.Data.SqlClient;
using System.Data;
using TLECrawler.Domain.Common.Configurations;

namespace TLECrawler.Application.DAL;

public interface ITLEDBFactory
{
    public SqlConnection InitializeConnection();
    public SqlCommand CreateSqlCommand(SqlConnection connection, string sqlCommand = "", int timeout = 30);
    public SqlCommand CreateSqlCommand(SqlConnection connection, SqlTransaction transaction, string sqlCommand = "", int timeout = 60);
    public SqlParameter CreateSqlParameter(string name, SqlDbType type, object value, ParameterDirection? direction = null);
    public void ExecuteTransaction(SqlConnection connection, string sqlCommand);
    public int ExecuteQuery(SqlConnection connection, string query);
    public Task<int> ExecuteQueryAsync(SqlConnection connection, string query);

    public DataBaseSettings GetDatabaseCredentials();

    public void ExecuteStoredProcedure(SqlConnection connection, string procedureName, SqlParameter[] sqlParameters, SqlTransaction? transaction = null);
    public Task<int> ExecuteStoredProcedureAsync(SqlConnection connection, string procedureName, SqlParameter[] sqlParameters, SqlTransaction? transaction = null);
}
