using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace RoutePlanner_Api.Data;

public class VRPConnectionFactory(IConfiguration config)
{
    private readonly string _connectionstring = config.GetConnectionString("VRP") ?? throw new ArgumentNullException("Connection String VRP is empty.");

    public DbConnection CreateConnection()
    {
        return new SqlConnection(_connectionstring);
    }
}
