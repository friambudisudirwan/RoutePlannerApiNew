using Dapper;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Services;

public class GPSBService
(
    GPSBConnectionFactory conn
)
{
    private readonly GPSBConnectionFactory _conn = conn;

    public async Task<List<GeofenceArea>> GetGPSBGeofences(int company_id, CancellationToken cancellationToken)
    {
        using var conn = _conn.CreateConnection();

        const string sql = @"SELECT id, code, name, points, lon, lat
                             FROM tbl_geofence_area WITH(NOLOCK)
                             WHERE company_id = @company_id AND is_deleted = 0 AND enabled_status = 1";
        var cmd = new CommandDefinition(sql, new { company_id }, cancellationToken: cancellationToken);
        var geofences = await conn.QueryAsync<GeofenceArea>(cmd);

        return [.. geofences];
    }


}
