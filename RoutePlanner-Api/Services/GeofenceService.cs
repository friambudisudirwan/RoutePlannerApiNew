using System.Data;
using RoutePlanner_Api.Data;

namespace RoutePlanner_Api.Services;

public class GeofenceService
(
    GPSBConnectionFactory gpsb
)
{
    private readonly GPSBConnectionFactory _gpsb = gpsb;

    public async Task<object> GetGeofencesByCode(string[] geo_code)
    {
        using var conn = _gpsb.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

        var sql = "";
        return new { };
    }
}
