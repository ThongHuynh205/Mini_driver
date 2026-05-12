using Microsoft.Data.SqlClient;

namespace Mini_driver.Server.Database;

public static class DbConnectionFactory
{
    public static SqlConnection Create()
    {
        return new SqlConnection(
            "Server=.;Database=Mini_driver;Trusted_Connection=True;TrustServerCertificate=True");
    }
}