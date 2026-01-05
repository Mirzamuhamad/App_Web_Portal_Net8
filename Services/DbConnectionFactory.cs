using Microsoft.Data.SqlClient;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connString;

    public DbConnectionFactory(IConfiguration config)
    {
        // _connString = config.GetConnectionString("DBConnection"); //v1 tanpa possible null

         if (config == null) throw new ArgumentNullException(nameof(config));
        _connString = config.GetConnectionString("DBConnection")
            ?? throw new InvalidOperationException("Connection string 'DBConnection' not found."); //v2 dengan possible null
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connString);
    }
}
