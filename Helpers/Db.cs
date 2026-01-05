using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public static class Db
{
    private static string? _connectionString; // tanda ? karena diinisialisasi di luar constructor dan bisa null jadi di tambah ? untuk menghindari warning nullable

    public static void Configure(IConfiguration config)
    {
        // _connectionString = config.GetConnectionString("DBConnection"); //v1 tanpa possible null

        if (config == null) throw new ArgumentNullException(nameof(config));
        _connectionString = config.GetConnectionString("DBConnection")
            ?? throw new InvalidOperationException("Connection string 'DBConnection' not found.");
    }

    public static SqlConnection Connect()
    {
        // return new SqlConnection(_connectionString); //v1 tanpa possible null

        var cs = _connectionString ?? throw new InvalidOperationException("Database not configured. Call Db.Configure(...) with a valid connection string first.");
        return new SqlConnection(cs); //v2 dengan possible null untuk menghindari warning nullable
    }
}
