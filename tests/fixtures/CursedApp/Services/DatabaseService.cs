using Microsoft.Data.SqlClient;

namespace CursedApp.Services;

// RWS-SEC-004: hardcoded connection string with password in source code
public class DatabaseService
{
    private const string ConnectionString =
        "Server=prod-db.cursedapp.internal;Initial Catalog=CursedDb;User Id=sa;Password=Sup3rS3cr3t!";

    public SqlConnection GetConnection() => new(ConnectionString);
}
