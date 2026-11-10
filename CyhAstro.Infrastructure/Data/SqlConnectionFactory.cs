using Microsoft.Data.SqlClient;
using System.Data;

namespace CyhAstro.Infrastructure.Data;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
        DapperTypeHandlers.Register();
    }

    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}
