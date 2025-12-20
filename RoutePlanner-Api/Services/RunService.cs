using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text;
using Dapper;
using RestSharp;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Models;
using Microsoft.Data.SqlClient;

namespace RoutePlanner_Api.Services;

public class RunService
(
    IConfiguration config,
    ILogger<RunService> logger,
    IBrokerService brokerService,
    VRPConnectionFactory vrp,
    UserIdentityService userIdentity
)
{
    private readonly ILogger<RunService> _logger = logger;
    private readonly VRPConnectionFactory _vrp = vrp;
    private readonly IBrokerService _brokerServie = brokerService;
    private readonly dynamic _brokerConfig = config.GetSection("RabbitMQConfig");
    private readonly UserIdentityService _userIdentity = userIdentity;
    private readonly string _vtsApiUrl = config.GetSection("Configs")["VtsApiUrl"] ?? throw new ArgumentNullException("Vts Api Url is empty");

    public async Task<List<string>> CreateRunsheets(ParamCreateRunsheets param, CancellationToken cancellationToken)
    {

        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);
        using var trx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var list_runid = new List<string>();
            var user_id = _userIdentity.GetUserId();
            var company_id = _userIdentity.GetCompanyId();

            foreach (var pool in param.Data)
            {
                var cmd_run_id = new CommandDefinition("sp_get_runid", commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);
                var run_id = await conn.QueryFirstOrDefaultAsync<string>(cmd_run_id) ?? throw new InvalidOperationException("Failed when generating RunID. Internal server error.");

                // ** insert pool
                var p = new DynamicParameters();
                p.Add("@runid", run_id, DbType.String, ParameterDirection.Input);
                p.Add("@poolid", pool.PoolID, DbType.String, ParameterDirection.Input);
                p.Add("@poolname", pool.PoolName.Replace("'", "''"), DbType.String, ParameterDirection.Input);
                p.Add("@starttime", pool.StartTime.ToString("yyyy-MM-dd HH:mm:ss"), DbType.String, ParameterDirection.Input);
                p.Add("@startlong", pool.StartLong, DbType.String, ParameterDirection.Input);
                p.Add("@startlat", pool.StartLat, DbType.String, ParameterDirection.Input);
                p.Add("@maxtimeidle", pool.MaxTimeIdle, DbType.Int32, ParameterDirection.Input);
                p.Add("@usrupd", user_id, DbType.String, ParameterDirection.Input);

                var cmd = new CommandDefinition("sp_api_run_insert_pool", parameters: p, commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);
                if (await conn.ExecuteAsync(cmd) < 1) throw new InvalidOperationException($"Failed when saving pool for pool id: {pool.PoolID}.");

                // ** insert car
                var seq_car = 1;
                foreach (var car in pool.Cars)
                {
                    var p_car = new DynamicParameters();
                    p_car.Add("@runid", run_id, DbType.String, ParameterDirection.Input);
                    p_car.Add("@seqno", seq_car, DbType.Int32, ParameterDirection.Input);
                    p_car.Add("@carid", car.CarID, DbType.String, ParameterDirection.Input);
                    p_car.Add("@cardesc", car.CarDesc, DbType.String, ParameterDirection.Input);
                    p_car.Add("@policeno", car.PoliceNo, DbType.String, ParameterDirection.Input);
                    p_car.Add("@capacity", car.Capacity, DbType.String, ParameterDirection.Input);
                    p_car.Add("@workingmin", car.WorkingTime.ToString(), DbType.String, ParameterDirection.Input);
                    p_car.Add("@minresttime", $"{pool.StartTime:yyyy-MM-dd} {car.MinRestTime}", DbType.String, ParameterDirection.Input);
                    p_car.Add("@resttime", car.RestTime, DbType.Int32, ParameterDirection.Input);
                    p_car.Add("@usrupd", user_id, DbType.String, ParameterDirection.Input);

                    var cmd_car = new CommandDefinition("sp_api_run_insert_car", parameters: p_car, commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);
                    if (await conn.ExecuteAsync(cmd_car) < 1) throw new InvalidOperationException($"Failed when saving car for pool id: {pool.PoolID}, car id: {car.CarID}");

                    seq_car++;
                }

                // ** insert trip
                var seq_trip = 1;
                foreach (var trip in pool.Trips)
                {
                    var p_trip = new DynamicParameters();
                    p_trip.Add("@runid", run_id, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@seqno", seq_trip, DbType.Int32, ParameterDirection.Input);
                    p_trip.Add("@tripid", trip.TripId, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@tripname", trip.TripName, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@trip_long", trip.TripLong, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@trip_lat", trip.TripLat, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@time_open", $"{pool.StartTime:yyyy-MM-dd} {trip.TimeOpen}", DbType.String, ParameterDirection.Input);
                    p_trip.Add("@time_close", $"{pool.StartTime:yyyy-MM-dd} {trip.TimeClose}", DbType.String, ParameterDirection.Input);
                    p_trip.Add("@time_wait", trip.TimeWait, DbType.Int32, ParameterDirection.Input);
                    p_trip.Add("@time_operation", trip.TimeOperation, DbType.Int32, ParameterDirection.Input);
                    p_trip.Add("@capacity", trip.Capacity, DbType.Double, ParameterDirection.Input);
                    p_trip.Add("@balance", trip.Balance, DbType.Double, ParameterDirection.Input);
                    p_trip.Add("@layananid", trip.LayananID, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@TripType", trip.TripType, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@MetodeHitung", trip.MetodeHitung, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@Siklus", trip.Siklus, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@TrxID", trip.TrxID, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@ZoneCode", trip.ZoneCode, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@RegionCode", trip.RegionCode, DbType.String, ParameterDirection.Input);
                    p_trip.Add("@is_dv", 0, DbType.Int32, ParameterDirection.Input);
                    p_trip.Add("@parentid", "", DbType.String, ParameterDirection.Input);
                    p_trip.Add("@usrupd", user_id, DbType.String, ParameterDirection.Input);

                    var cmd_trip = new CommandDefinition("sp_api_run_insert_trip", parameters: p_trip, commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);
                    if (await conn.ExecuteAsync(cmd_trip) < 1) throw new InvalidOperationException($"Failed when saving trip for pool id: {pool.PoolID}, trip id: {trip.TripId}");

                    seq_trip++;
                }

                list_runid.Add(run_id);
            }

            await trx.CommitAsync(cancellationToken);


            // ** hit broker rabbitmq buat jalanin background service
            foreach (var runid in list_runid)
            {
                await _brokerServie.PublishMessage
                (
                    exchange: _brokerConfig["ExchangeName"],
                    routing_key: _brokerConfig["RoutingKey"],
                    message: JsonConvert.SerializeObject(list_runid.GroupBy(x => x).Select(x => new
                    {
                        runid = runid,
                        userid = user_id,
                        start_time = DateTime.Now,
                        company_id
                    }))
                );
            }

            return list_runid;
        }
        catch (InvalidOperationException ex)
        {
            await trx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Invalid operation exception.");
            throw;
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Internal server error");
            throw;
        }
        finally
        {
            await trx.DisposeAsync();
        }
    }

    public async Task<List<long>> IntegrateRunsheets(ParamIntegrateRunsheets param, CancellationToken cancellationToken)
    {
        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);
        using var trx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var company_id = _userIdentity.GetCompanyId();
            var user_id = _userIdentity.GetUserId();
            var token_h2h = await _userIdentity.GetTokenH2H(cancellationToken);

            var list_do_id = new List<long>();

            foreach (var run in param.data)
            {
                // ** cek apakah dari run dan car sudah ke-route
                var sql = @"SELECT TOP 1 RunID FROM api_trx_route WITH(NOLOCK)
                            WHERE runid = @runid AND carid = @carid AND UsrUpd = @user_id";
                var cmd_check = new CommandDefinition(sql, new { runid = run.RunId, carid = run.CarId, user_id }, commandType: CommandType.Text, transaction: trx, cancellationToken: cancellationToken);
                var validate_route = await conn.QueryFirstOrDefaultAsync<string>(cmd_check);

                if (string.IsNullOrEmpty(validate_route)) throw new CreateRunsheetException("Route mobil tidak ditemukan.");

                // ** cek apakah route sudah terintegrasi
                sql = @"SELECT TOP 1 RunID FROM api_trx_route WITH(NOLOCK)
                        WHERE runid = @runid AND carid = @carid AND UsrUpd = @user_id AND ISNULL(IsPostDO, 0) = 1";
                var cmd_check2 = new CommandDefinition(sql, new { runid = run.RunId, carid = run.CarId, user_id }, commandType: CommandType.Text, transaction: trx, cancellationToken: cancellationToken);
                var validate_route2 = await conn.QueryFirstOrDefaultAsync<string>(cmd_check2);

                if (!string.IsNullOrEmpty(validate_route2)) throw new CreateRunsheetException("Route mobil sudah pernah diintegrasikan ke TMS EasyGo.");

                // ** begin post do
                var p = new DynamicParameters();
                p.Add("@runid", run.RunId, DbType.String, ParameterDirection.Input);
                p.Add("@carid", run.CarId, DbType.String, ParameterDirection.Input);

                var cmd = new CommandDefinition("sp_posting_do_tms", p, commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);
                var fetch_do_post_param = await conn.QueryFirstOrDefaultAsync<string>(cmd) ?? throw new Exception("No data when preparing to integrate to TMS EasyGo. Internal server error");
                var do_post_param = JsonConvert.DeserializeObject<ParamCreateDoByGeoCode>(fetch_do_post_param);

                // ** update route to IsPostDo = 1
                sql = @"UPDATE api_trx_route SET IsPostDO = 1
                        WHERE RunId = @runid AND CarID = @carid";
                var cmd3 = new CommandDefinition(sql, new { runid = run.RunId, carid = run.CarId }, commandType: CommandType.Text, transaction: trx, cancellationToken: cancellationToken);
                var update_route_ispostdo_status = await conn.ExecuteAsync(cmd3);

                // **hit vts api create do by code
                var client = new RestClient(_vtsApiUrl);
                var request = new RestRequest("/api/do/AddOrUpdateDOV1ByGeoCode", Method.Post);

                // Header Token
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Token", token_h2h);

                var request_body = JsonConvert.SerializeObject(do_post_param);
                request.AddParameter(
                    "application/json",
                    request_body,
                    ParameterType.RequestBody
                );

                var response = await client.ExecuteAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode) throw new Exception(response.ErrorMessage);

                var responseData = JsonConvert.DeserializeObject<VtsApiResponseBase<DoIdData>>(response.Content ?? "") ?? throw new ArgumentNullException("Failed when integrating to TMS EasyGO");
                if (responseData.ResponseCode != 1) throw new Exception(responseData.ResponseMessage);

                list_do_id.Add(responseData.Data?.do_id ?? 0);
            }

            // **commit trx
            await trx.CommitAsync(cancellationToken);
            return list_do_id.Where(x => x > 0).ToList();
        }
        catch (InvalidOperationException)
        {
            await trx.RollbackAsync(cancellationToken);
            throw;
        }
        catch (CreateRunsheetException)
        {
            await trx.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception)
        {
            await trx.RollbackAsync(cancellationToken);
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
        var sql = @"INSERT INTO api_mst_trip (RunID, SeqNo, TripID, TripName, TripLong, TripLat, CityName,
                                              Capacity, Balance, TrxID, Warehouse, BU, PL, PS, StorageType, 
                                              NoSo, CodeCustomer, Segment, TotalQty, TotalGrossVolume, IsAllowRoute,
                                              IsValidLonLat, UsrUpd, DtmUpd, source_data)
                    VALUES ('', @seqno, @tripid, @tripname, @triplong, @triplat, @cityname,
                            @capacity, @balance, @trxid, @poolid, @bu, @pl, @ps, @storagetype, 
                            @noso, @codecustomer, @segment, @totalqty, @totalgrossvolume, 1,
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
