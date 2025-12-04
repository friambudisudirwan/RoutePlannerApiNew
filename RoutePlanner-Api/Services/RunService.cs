using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Models;

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

    // public async Task<(int status, string message, List<string> data)> CreateRunsheets
    // (
    //     ParamCreateRunsheets param,
    //     CancellationToken cancellationToken
    // )
    // {
    //     using var conn = _vrp.CreateConnection();
    //     if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);
    //     using var trx = await conn.BeginTransactionAsync(cancellationToken);

    //     // ** status 0 = initial, 500 = error, 200 = success 
    //     var status = 0;
    //     var message = "";

    //     var created_runid = new List<string>();

    //     try
    //     {
    //         // ** begin create runsheets
    //         foreach (var pool in param.Data)
    //         {
    //             var runid = await GetRunID(conn, trx, cancellationToken);

    //             var pPool = new DynamicParameters();
    //             pPool.Add("@runid", runid, DbType.String, ParameterDirection.Input);
    //             pPool.Add("@poolid", pool.PoolID, DbType.String, ParameterDirection.Input);
    //             pPool.Add("@poolname", pool.PoolName, DbType.String, ParameterDirection.Input);
    //             pPool.Add("@starttime", pool.StartTime, DbType.String, ParameterDirection.Input);
    //             pPool.Add("@startlong", pool.StartLong, DbType.String, ParameterDirection.Input);
    //             pPool.Add("@startlat", pool.StartLat, DbType.String, ParameterDirection.Input);
    //             pPool.Add("@maxtimeidle", pool.MaxTimeIdle, DbType.Int32, ParameterDirection.Input);
    //             pPool.Add("@usrupd", param.User.UserID, DbType.String, ParameterDirection.Input);

    //             var cmdPool = new CommandDefinition("sp_api_run_insert_pool", pPool, commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);

    //             if (await conn.ExecuteAsync(cmdPool) < 1) throw new InvalidOperationException($"Failed when creating pool for PoolID: {pool.PoolID}. No row affected. Internal server error.");

    //             // ** insert cars

    //             // ** insert trips

    //         }

    //         status = 200;
    //         message = "Runsheets created";
    //         await trx.CommitAsync(cancellationToken);
    //     }
    //     catch (InvalidOperationException ex)
    //     {
    //         await trx.RollbackAsync(cancellationToken);
    //         _logger.LogWarning(ex, "Failed when creating runsheet.");
    //         status = 500;
    //         message = ex.Message;
    //     }
    //     catch (Exception ex)
    //     {
    //         await trx.RollbackAsync(cancellationToken);
    //         _logger.LogError(ex, "Internal server error while creating runsheet.");
    //         status = 500;
    //         message = ex.Message;
    //     }
    //     finally
    //     {
    //         await trx.DisposeAsync();
    //     }

    //     return (status, message, created_runid);
    // }

    public async Task<List<string>> CreatePrambananRunsheets(ParamCreateRunsheetPrambanan param, CancellationToken cancellationToken)
    {
        var company_id = _userIdentity.GetCompanyId();
        var user_id = _userIdentity.GetUserId();
        var current_date_time = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

        try
        {
            // ** insert trips
            await InsertPrambananTrips
            (
                current_date_time,
                user_id ?? "",
                param.Data.Where(x => IsDouble(x.TripLong) && IsDouble(x.TripLat)).ToList(),
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
                                              Capacity, Balance, TrxID, Warehouse, BU, StorageType, Segment, TotalQty, TotalGrossVolume,
                                              UsrUpd, DtmUpd, source_data)
                    VALUES ('', @seqno, @tripid, @tripname, @triplong, @triplat,
                            @capacity, @balance, @trxid, @poolid, @bu, @storagetype, @segment, @totalqty, @totalgrossvolume,
                            @usrupd, @dtmupd, 'Api-Prambanan')";

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

    private static bool IsDouble(string input)
    {
        return double.TryParse(input, out _);
    }

    // private async Task InsertCars
    // (
    //     List<ApiMstCar> cars,
    //     SqlConnection conn,
    //     DbTransaction trx,
    //     CancellationToken cancellationToken
    // )
    // {

    // }
    // private async Task InsertTrips
    // (
    //     List<ApiMstTrip> trips,
    //     SqlConnection conn,
    //     DbTransaction trx,
    //     CancellationToken cancellationToken
    // )
    // {

    // }

    private static async Task<string> GetRunID(SqlConnection conn, DbTransaction trx, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition("sp_get_runid", parameters: null, commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);

        var runid = await conn.QueryFirstOrDefaultAsync<string>(cmd) ?? throw new InvalidOperationException("Failed when getting RunID from database. Internal server error.");
        return runid;
    }
}
