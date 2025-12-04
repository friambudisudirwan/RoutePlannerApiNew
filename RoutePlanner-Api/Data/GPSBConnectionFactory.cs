using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace RoutePlanner_Api.Data;

public class GPSBConnectionFactory(IConfiguration config)
{
    private readonly string _connectionstring = config.GetConnectionString("GPSB") ?? throw new ArgumentNullException("Connection String GPSB is empty.");

    public DbConnection CreateConnection()
    {
        return new SqlConnection(_connectionstring);
    }
}
