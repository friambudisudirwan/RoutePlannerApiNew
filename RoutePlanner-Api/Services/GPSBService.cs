using System.Data.Common;
using Dapper;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Services;

public class GPSBService
(
    GPSBConnectionFactory conn
)
{
    private readonly GPSBConnectionFactory _conn = conn;

    public async Task<List<CarMaster>> GetGPSBVehicles
    (
        int company_id,
        List<string> list_police_no,
        CancellationToken cancellationToken
    )
    {
        using var conn = await _conn.CreateConnection();

        const string sql = @"SELECT car_plate, msisdn, vehicle_id, driver_id FROM car_master WITH(NOLOCK)
                             WHERE company_id = @company_id AND car_plate IN @list_vehicle";
        var cmd = new CommandDefinition(sql, new { company_id, list_vehicle = list_police_no }, cancellationToken: cancellationToken);
        var list_gpsb_vehicle = await conn.QueryAsync<CarMaster>(cmd);

        return [.. list_gpsb_vehicle];
    }

    public async Task<CarMaster> FindGPSBVehicleByGpsSn
    (
        int company_id,
        string gps_sn,
        CancellationToken cancellationToken
    )
    {
        using var conn = await _conn.CreateConnection();

        const string sql = @"SELECT TOP 1 car_plate, msisdn, vehicle_id, driver_id FROM car_master WITH(NOLOCK)
                             WHERE company_id = @company_id AND msisdn = @gps_sn";
        var vehicle = await conn.QueryFirstOrDefaultAsync<CarMaster>(new CommandDefinition
        (
            sql, new { company_id, gps_sn },
            cancellationToken: cancellationToken
        )) ?? throw new CustomException($"Kendaraan dengan GPS SN: {gps_sn} tidak dapat ditemukan", StatusCodes.Status404NotFound);

        return vehicle;
    }

    public async Task<List<Client>> GetGPSBClients
    (
        int company_id,
        CancellationToken cancellationToken
    )
    {
        using var conn = await _conn.CreateConnection();

        const string sql = @"SELECT client_id, code, name, address FROM tbl_client WITH(NOLOCK)
                             WHERE company_id = @company_id AND is_enabled = 1";
        var cmd = new CommandDefinition(sql, new { company_id }, cancellationToken: cancellationToken);
        var list_gpsb_client = await conn.QueryAsync<Client>(cmd);

        return [.. list_gpsb_client];
    }

    public async Task SaveClient
    (
        int company_id,
        string code,
        string name,
        string address,
        DateTime current_datetime,
        DbConnection conn,
        DbTransaction trx,
        CancellationToken cancellationToken
    )
    {
        // cek terlebih dahulu apakah client sudah ada (validasi by code)
        var sql = @"SELECT TOP 1 client_id FROM tbl_client WITH(NOLOCK)
                   WHERE company_id = @company_id AND code = @code AND is_enabled = 1";
        var existing_client_id = await conn.QueryFirstOrDefaultAsync<int>(new CommandDefinition(sql, new { company_id, code }, transaction: trx, cancellationToken: cancellationToken));
        // terhitung sebagai unhandled exception
        if (existing_client_id > 0) throw new CustomException($"Client data already exists, with code = {code}, name = {name}", StatusCodes.Status500InternalServerError);

        // simpan client
        sql = @"INSERT INTO tbl_client (company_id, code, name, address, is_enabled, usrcrt, dtmcrt)
                VALUES (@company_id, @code, @name, @address, 1, 4, @current_datetime)";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { company_id, code, name, address, current_datetime }, transaction: trx, cancellationToken: cancellationToken));
    }

    public async Task<List<GeofenceArea>> GetGPSBGeofences
    (
        int company_id,
        CancellationToken cancellationToken
    )
    {
        using var conn = await _conn.CreateConnection();

        const string sql = @"SELECT id, code, name, address, relative_name, color, alert, 
                                    shapeid, points, lon, lat, lon1, stamp, companyid, categoryid, 
                                    geo_type, enabled_status, alert_telegram, alert_email, alert_over_time, is_deleted, usrupd, dtmupd
                            FROM tbl_geofence_area WITH(NOLOCK)
                            WHERE companyid = @company_id AND enabled_status = 1 AND is_deleted = 0";
        var cmd = new CommandDefinition(sql, new { company_id }, cancellationToken: cancellationToken);
        var list_gpsb_geofence = await conn.QueryAsync<GeofenceArea>(cmd);

        return [.. list_gpsb_geofence];
    }

    public async Task SaveGeofence
    (
        int company_id,
        string code,
        string name,
        string address,
        string relative_name,
        string lon,
        string lat,
        DateTime current_datetime,
        DbConnection conn,
        DbTransaction trx,
        CancellationToken cancellationToken
    )
    {
        // cek terlebih dahulu apakah geofence sudah ada (validasi by code)
        var sql = @"SELECT TOP 1 id FROM tbl_geofence_area WITH(NOLOCK)
                   WHERE companyid = @company_id AND code = @code AND is_deleted = 0 AND enabled_status = 1";
        var existing_geo_id = await conn.QueryFirstOrDefaultAsync<int>(new CommandDefinition(sql, new { company_id, code }, transaction: trx, cancellationToken: cancellationToken));
        // terhitung sebagai unhandled exception
        if (existing_geo_id > 0) throw new CustomException($"Geofence data already exists, with code = {code}, name = {name}", StatusCodes.Status500InternalServerError);

        // simpan geofence
        sql = @"INSERT INTO tbl_geofence_area (code, name, address, relative_name, color, alert,
                                                            shapeid, points, lon, lat, lon1, stamp, companyid, categoryid, 
                                                            geo_type, enabled_status, alert_telegram, alert_email, alert_over_time, 
                                                            is_deleted, usrupd, dtmupd)
                            VALUES (@code, @name, @address, @relative_name, @color, @alert,
                                    @shapeid, @points, @lon, @lat, @lon1, @stamp, @companyid, @categoryid, 
                                    @geo_type, @enabled_status, @alert_telegram, @alert_email, @alert_over_time,
                                    @is_deleted, @usrupd, @dtmupd)";
        var payload = new
        {
            code,
            name,
            address,
            relative_name,
            color = "0xff0000",
            alert = 0,
            shapeid = 1,
            points = $"{lon},{lat},100",
            lon = Convert.ToDouble(lon),
            lat = Convert.ToDouble(lat),
            lon1 = 100,
            stamp = current_datetime,
            companyid = company_id,
            categoryid = 2,
            geo_type = 6,
            enabled_status = 1,
            alert_telegram = string.Empty,
            alert_email = string.Empty,
            alert_over_time = 0,
            is_deleted = 0,
            usrupd = 4,
            dtmupd = current_datetime
        };
        var cmd = new CommandDefinition(sql, payload, transaction: trx, cancellationToken: cancellationToken);
        await conn.ExecuteAsync(cmd);
    }

    public async Task CreateOrder
    (
        int company_id,
        DateTime order_date,
        int client_id,
        int geo_id,
        double capacity,
        double balance,
        string pl,
        DateTime current_datetime,
        DbConnection conn,
        DbTransaction trx,
        CancellationToken cancellationToken
    )
    {
        // insert order header
        var order_no = await GenerateOrderNumber
        (
            company_id,
            order_date,
            conn,
            trx,
            cancellationToken
        );
        var sql = @"INSERT INTO tbl_order_header (order_no, order_date, company_id, type, total_value, client_id, 
                                                  remarks, pl, is_allow_route, is_enabled, dtmcrt, dtmupd)
                    OUTPUT INSERTED.order_id
                    VALUES (@order_no, @order_date, @company_id, @type, @total_value, @client_id,
                            @remarks, @pl, @is_allow_route, 1, @current_datetime, @current_datetime)";
        var cmd = new CommandDefinition(sql, new
        {
            order_no,
            order_date,
            company_id,
            type = 2,
            total_value = balance,
            client_id,
            remarks = "Route Planner Integration",
            pl,
            is_allow_route = 1,
            current_datetime
        }, transaction: trx, cancellationToken: cancellationToken);
        var order_id = await conn.ExecuteScalarAsync<int>(cmd);

        // insert order tujuan
        sql = @"INSERT INTO tbl_order_tujuan (order_id, seq, geo_id, dest_type, [weight], is_enabled, dtmcrt, dtmupd)
                VALUES (@order_id, @seq, @geo_id, @dest_type, @weight, 1, @current_datetime, @current_datetime)";
        var cmd2 = new CommandDefinition(sql, new
        {
            order_id,
            seq = 1,
            geo_id,
            dest_type = 1,
            weight = capacity,
            current_datetime
        }, transaction: trx, cancellationToken: cancellationToken);
        await conn.ExecuteAsync(cmd2);
    }

    private static async Task<string> GenerateOrderNumber
    (
        int company_id,
        DateTime order_date,
        DbConnection conn,
        DbTransaction trx,
        CancellationToken cancellationToken
    )
    {
        var prefix = $"SO{order_date:ddMMyy}";
        var next_sequence = 1;

        const string sql = @"SELECT TOP 1 order_no FROM tbl_order_header WITH(NOLOCK)
                             WHERE company_id = @company_id AND order_no LIKE @prefix AND is_enabled = 1
                             ORDER BY order_no DESC";
        var cmd = new CommandDefinition(sql, new { company_id, prefix = $"{prefix}%" }, transaction: trx, cancellationToken: cancellationToken);
        var last_order_no = await conn.QueryFirstOrDefaultAsync<string>(cmd);

        if (!string.IsNullOrWhiteSpace(last_order_no)
            && last_order_no.StartsWith(prefix, StringComparison.Ordinal)
            && last_order_no.Length == prefix.Length + 5
            && int.TryParse(last_order_no.AsSpan(prefix.Length), out var last_seq))
        {
            next_sequence = last_seq + 1;
        }

        return $"{prefix}{next_sequence:D5}";
    }
}
