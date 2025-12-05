using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Dapper;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Dtos;

namespace RoutePlanner_Api.Services;

public class RunService
(
    IConfiguration config,
    ILogger<RunService> logger,
    VRPConnectionFactory vrp,
    UserIdentityService userIdentity
)
{
    private readonly ILogger<RunService> _logger = logger;
    private readonly VRPConnectionFactory _vrp = vrp;
    private readonly UserIdentityService _userIdentity = userIdentity;
    private readonly string _pathRouteService = config.GetSection("Configs")["PathRouteService"] ?? throw new ArgumentNullException("Path Route service is empty");

    public async Task<List<string>> CreatePrambananRunsheets(ParamCreateRunsheetPrambanan param, CancellationToken cancellationToken)
    {
        var company_id = _userIdentity.GetCompanyId();
        var user_id = _userIdentity.GetUserId();
        var current_date_time = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

        try
        {
            // ** list valid trip
            var map_trips = param.Data.Where(x => IsInIndonesiaValid(x.TripLat.Trim(), x.TripLong.Trim())).Select(x => x with
            {
                TripLong = x.TripLong.Trim(),
                TripLat = x.TripLat.Trim(),
                IsValidLonLat = 1
            }).ToList();

            // ** list invalid trip
            map_trips.AddRange(param.Data.Where(x => !IsInIndonesiaValid(x.TripLat.Trim(), x.TripLong.Trim())).Select((x, i) => x with
            {
                TripLong = string.Empty,
                TripLat = string.Empty,
                IsValidLonLat = 0
            }));

            // ** insert trips
            await InsertPrambananTrips
            (
                current_date_time,
                user_id ?? "",
                map_trips,
                conn,
                cancellationToken
            );

            // ** pre run inserted trips
            var list_runid = await PrerunPrambananTrips
            (
                company_id,
                user_id ?? "",
                param.StartTime,
                current_date_time,
                conn,
                cancellationToken
            );

            // ** pre run po
            await PrerunPrambananPo
            (
                list_runid,
                conn,
                cancellationToken
            );

            // ** hit run service
            await BeginRun
            (
                user_id ?? "",
                param.StartTime,
                list_runid,
                conn,
                cancellationToken
            );

            return list_runid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Internal server error");
            throw;
        }
    }

    private static async Task PrerunPrambananPo
    (
        List<string> list_runid,
        DbConnection conn,
        CancellationToken cancellationToken
    )
    {
        foreach (var runid in list_runid)
        {
            var p = new DynamicParameters();
            p.Add("@runid", runid, DbType.String, ParameterDirection.Input);

            var cmd = new CommandDefinition("sp_prerun_prambanan_po", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(cmd);
        }
    }

    private async Task BeginRun
    (
        string UserId,
        DateTime start_time,
        List<string> list_runid,
        DbConnection conn,
        CancellationToken cancellationToken
    )
    {
        foreach (var runid in list_runid)
        {
            var p = new DynamicParameters();
            p.Add("@runid", runid, DbType.String, ParameterDirection.Input);
            p.Add("@start_Time", start_time, DbType.DateTime, ParameterDirection.Input);

            var cmd = new CommandDefinition("sp_delete_car_already_routed", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(cmd);

            await Task.Run(() =>
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _pathRouteService,
                    Arguments = $"{runid}|{UserId}|0",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = Process.Start(processInfo);
                process?.WaitForExit();
            }, cancellationToken);
        }
    }

    private static async Task InsertPrambananTrips
    (
        DateTime current_date_time,
        string UserId,
        List<ParamTripPrambanan> trips,
        DbConnection conn,
        CancellationToken cancellationToken
    )
    {
        var map_trips = trips.Select((x, i) => x with { SeqNo = i + 1, UsrUpd = UserId, DtmUpd = current_date_time });
        var sql = @"INSERT INTO api_mst_trip (RunID, SeqNo, TripID, TripName, TripLong, TripLat,
                                              Capacity, Balance, TrxID, Warehouse, BU, StorageType, 
                                              NoSo, CodeCustomer, Segment, TotalQty, TotalGrossVolume,
                                              IsValidLonLat, UsrUpd, DtmUpd, source_data)
                    VALUES ('', @seqno, @tripid, @tripname, @triplong, @triplat,
                            @capacity, @balance, @trxid, @poolid, @bu, @storagetype, 
                            @noso, @codecustomer, @segment, @totalqty, @totalgrossvolume,
                            @isvalidlonlat, @usrupd, @dtmupd, 'Api-Prambanan')";

        var cmd = new CommandDefinition(sql, map_trips, commandType: CommandType.Text, cancellationToken: cancellationToken);
        await conn.ExecuteAsync(cmd);
    }

    private static async Task<List<string>> PrerunPrambananTrips
    (
        int company_id,
        string user_id,
        DateTime start_time,
        DateTime current_date_time,
        DbConnection conn,
        CancellationToken cancellationToken
    )
    {
        var p = new DynamicParameters();
        p.Add("@company_id", company_id, DbType.Int32, ParameterDirection.Input);
        p.Add("@usrupd", user_id, DbType.String, ParameterDirection.Input);
        p.Add("@dtmupd", current_date_time, DbType.DateTime, ParameterDirection.Input);
        p.Add("@start_time", start_time, DbType.DateTime, ParameterDirection.Input);

        var cmd = new CommandDefinition("sp_prerun_prambanan", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        await conn.ExecuteAsync(cmd);

        var sql = @"SELECT runid FROM api_mst_trip WITH(NOLOCK)
                    WHERE usrupd = @user_id AND dtmupd = @current_date_time AND runid != ''
                    GROUP BY runid";
        var cmd2 = new CommandDefinition(sql, new { user_id, current_date_time }, commandType: CommandType.Text, cancellationToken: cancellationToken);
        var list_runid = await conn.QueryAsync<string>(cmd2);

        return [.. list_runid];
    }

    private static bool IsValidLongLat(string input)
    {
        return double.TryParse(input.Trim(), out _);
    }

    private static bool IsInIndonesia(double lat, double lon)
    {
        return lat >= -11 && lat <= 6 &&
               lon >= 95 && lon <= 141;
    }

    private static bool IsInIndonesiaValid(string lat, string lon)
    {
        if (IsValidLongLat(lat) && IsValidLongLat(lon))
        {
            return IsInIndonesia(Convert.ToDouble(lat), Convert.ToDouble(lon));
        }
        else
        {
            return false;
        }
    }
}
