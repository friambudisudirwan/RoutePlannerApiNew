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
using System.Globalization;

namespace RoutePlanner_Api.Services;

public class RunService
(
    IConfiguration config,
    ILogger<RunService> logger,
    IBrokerService brokerService,
    VRPConnectionFactory vrp,
    GPSBConnectionFactory gpsb,
    GPSBService gpsb_service,
    IntegrateService integrate_service,
    UserIdentityService userIdentity
)
{
    private readonly ILogger<RunService> _logger = logger;
    private readonly VRPConnectionFactory _vrp = vrp;
    private readonly GPSBConnectionFactory _gpsb = gpsb;
    private readonly IBrokerService _brokerServie = brokerService;
    private readonly GPSBService _gpsb_service = gpsb_service;
    private readonly IntegrateService _integrate_service = integrate_service;
    private readonly dynamic _brokerConfig = config.GetSection("RabbitMQConfig");
    private readonly UserIdentityService _userIdentity = userIdentity;
    private readonly string _vtsApiUrl = config.GetSection("Configs")["VtsApiUrl"] ?? throw new ArgumentNullException("Vts Api Url is empty");

    public async Task<List<string>> CreateRunsheets(ParamCreateRunsheets param, CancellationToken cancellationToken)
    {

        using var conn = _vrp.CreateConnection();
        // if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);
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

                var cmd_queue = new CommandDefinition(@"UPDATE api_mst_pool SET InQueue = 1 WHERE RunID = @runid", new { runid = run_id }, transaction: trx, cancellationToken: cancellationToken);
                await conn.ExecuteAsync(cmd_queue);

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

        using var conn_gpsb = await _gpsb.CreateConnection();
        if (conn_gpsb.State == ConnectionState.Closed) await conn_gpsb.OpenAsync(cancellationToken);
        using var trx_gpsb = await conn_gpsb.BeginTransactionAsync(cancellationToken);

        try
        {
            var company_id = _userIdentity.GetCompanyId();
            var user_id = _userIdentity.GetUserId();
            var current_datetime = DateTime.Now;
            var token_h2h = await _userIdentity.GetTokenH2H(cancellationToken);

            var list_do_id = new List<long>();

            foreach (var run in param.data)
            {

                // ** cek apakah dari run dan car sudah ke-route
                var sql = @"SELECT TOP 1 RunID FROM api_trx_route WITH(NOLOCK)
                            WHERE runid = @runid AND carid = @carid AND UsrUpd = @user_id";
                var cmd_check = new CommandDefinition(sql, new { runid = run.RunId, carid = run.CarId, user_id }, commandType: CommandType.Text, transaction: trx, cancellationToken: cancellationToken);
                var validate_route = await conn.QueryFirstOrDefaultAsync<string>(cmd_check);

                if (string.IsNullOrEmpty(validate_route)) throw new CustomException("Route mobil tidak ditemukan.", StatusCodes.Status404NotFound);

                // ** cek apakah route sudah terintegrasi
                sql = @"SELECT TOP 1 RunID FROM api_trx_route WITH(NOLOCK)
                        WHERE runid = @runid AND carid = @carid AND UsrUpd = @user_id AND ISNULL(IsPostDO, 0) = 1";
                var cmd_check2 = new CommandDefinition(sql, new { runid = run.RunId, carid = run.CarId, user_id }, commandType: CommandType.Text, transaction: trx, cancellationToken: cancellationToken);
                var validate_route2 = await conn.QueryFirstOrDefaultAsync<string>(cmd_check2);

                if (!string.IsNullOrEmpty(validate_route2)) throw new CustomException("Route mobil sudah pernah diintegrasikan ke TMS EasyGo.", StatusCodes.Status422UnprocessableEntity);

                // ** preparasi integrasi data yang dibutuhkan untuk tms easygo
                await PrerunIntegration
                (
                    company_id: company_id,
                    runid: run.RunId,
                    current_datetime: current_datetime,
                    conn_vrp: conn,
                    trx_vrp: trx,
                    conn_gpsb: conn_gpsb,
                    trx_gpsb: trx_gpsb,
                    cancellationToken: cancellationToken
                );

                // ** create post do payload
                var fetch_pool = GetPool(run.RunId, conn, trx, cancellationToken);
                var fetch_trips = GetTrips(run.RunId, conn, trx, cancellationToken);
                var fetch_vehicle = _gpsb_service.FindGPSBVehicleByGpsSn(company_id, run.CarId, cancellationToken);

                await Task.WhenAll([fetch_pool, fetch_vehicle]);

                var order_date = fetch_pool.Result.StartTime;
                var car_plate = fetch_vehicle.Result.car_plate;
                var driver_id = fetch_vehicle.Result.driver_id;
                var geo_asal_code = fetch_pool.Result.PoolID;
                var trips = fetch_trips.Result;

                // ** hit vtsapi untuk create do
                var do_payload = new
                {
                    do_id = 0,
                    tgl_do = order_date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    car_plate,
                    allow_multiple_do = 1,
                    no_do = $"{run.RunId}-{DateTime.Now:yyyyMMddHHmmss}",
                    note = "Auto posting from route plan.",
                    opsi_complete = 4,
                    driver_id,
                    geo_asal = new List<object>() { new { code = geo_asal_code } },
                    geo_tujuan = trips.Select(x => new
                    {
                        code = x.TripId,
                        no_sj = x.TrxID
                    })
                };

                var do_id = await _integrate_service.AddOrUpdateDOV1ByGeoCode(token_h2h, do_payload, cancellationToken);

                // ** update route to IsPostDo = 1
                sql = @"UPDATE api_trx_route SET IsPostDO = 1
                        WHERE RunId = @runid AND CarID = @carid";
                await conn.ExecuteAsync(new CommandDefinition
                (
                    sql, new { runid = run.RunId, carid = run.CarId }, transaction: trx, cancellationToken: cancellationToken
                ));

                list_do_id.Add(do_id);
            }

            // **commit trx
            await trx.CommitAsync(cancellationToken);
            await trx_gpsb.CommitAsync(cancellationToken);
            return [.. list_do_id.Where(x => x > 0)];
        }
        catch (InvalidOperationException)
        {
            await trx.RollbackAsync(cancellationToken);
            await trx_gpsb.RollbackAsync(cancellationToken);
            throw;
        }
        catch (CreateRunsheetException)
        {
            await trx.RollbackAsync(cancellationToken);
            await trx_gpsb.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception)
        {
            await trx.RollbackAsync(cancellationToken);
            await trx_gpsb.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await trx.DisposeAsync();
            await trx_gpsb.DisposeAsync();

            if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            if (conn_gpsb.State == ConnectionState.Open) await conn_gpsb.CloseAsync();
        }
    }

    private async Task PrerunIntegration
    (
        int company_id,
        string runid,
        DateTime current_datetime,
        DbConnection conn_vrp,
        DbTransaction trx_vrp,
        DbConnection conn_gpsb,
        DbTransaction trx_gpsb,
        CancellationToken cancellationToken
    )
    {
        // ** prepare master client dengan gpsb
        // select dari api_mst_trip
        var sql = @"SELECT CodeCustomer AS code, TripName AS name, TripAddress AS address 
                    FROM api_mst_trip WITH(NOLOCK)
                    WHERE runid = @runid";
        var vrp_client = await conn_vrp.QueryAsync<Client>(new CommandDefinition
        (
            sql,
            new { runid },
            transaction: trx_vrp,
            cancellationToken: cancellationToken
        ));

        // select dari gpsb
        var gpsb_client = await _gpsb_service.GetGPSBClients(company_id, cancellationToken);
        // map client yang tidak ada di gpsb
        var not_in_gpsb_client = vrp_client.Where(x =>
                                    !gpsb_client.Select(y =>
                                        y.code.Trim().ToLower()
                                    ).Contains(x.code.Trim().ToLower())).GroupBy(x => x.code);
        // insert client
        foreach (var client in not_in_gpsb_client)
        {
            await _gpsb_service.SaveClient
            (
                company_id: company_id,
                code: client.Key,
                name: client.First().name,
                address: client.First().address,
                current_datetime: current_datetime,
                conn: conn_gpsb,
                trx: trx_gpsb,
                cancellationToken: cancellationToken
            );
        }

        // ** prepare master geofence dengan gpsb
        // select dari api_mst_pool dan api_mst_trip
        var fetch_pool = GetPool(runid, conn_vrp, trx_vrp, cancellationToken);
        var fetch_trips = GetTrips(runid, conn_vrp, trx_vrp, cancellationToken);

        await Task.WhenAll([fetch_pool, fetch_trips]);

        var pool = fetch_pool.Result;
        var trips = fetch_trips.Result;

        var vrp_geofence = new List<GeofenceArea>
        {
            // append pool terlebih dahulu sebagai geofence
            new(){code = pool.PoolID, name = pool.PoolName, address = string.Empty, lon = Convert.ToDouble(pool.StartLong), lat = Convert.ToDouble(pool.StartLat)},
        };
        // append trips sebagai geofence
        vrp_geofence.AddRange(trips.Select(x => new GeofenceArea
        {
            code = x.TripId,
            name = x.TripName,
            address = string.Empty,
            lon = Convert.ToDouble(x.TripLong),
            lat = Convert.ToDouble(x.TripLat)
        }));

        // select dari gpsb
        var gpsb_geofence = await _gpsb_service.GetGPSBGeofences(company_id, cancellationToken);
        var not_in_gpsb_geofence = vrp_geofence.Where(x => !gpsb_geofence.Select(y => y.code.Trim().ToLower()).Contains(x.code.Trim().ToLower())).GroupBy(x => x.code);

        // insert geofence
        foreach (var geofence in not_in_gpsb_geofence)
        {
            await _gpsb_service.SaveGeofence
            (
                company_id: company_id,
                code: geofence.First().code,
                name: geofence.First().name,
                address: geofence.First().address,
                relative_name: string.Empty,
                lon: geofence.First().lon.ToString(),
                lat: geofence.First().lat.ToString(),
                current_datetime: current_datetime,
                conn: conn_gpsb,
                trx: trx_gpsb,
                cancellationToken: cancellationToken
            );
        }

        // ** integrasi order ke gpsb
        // ambil starttime pool sebagai parameter order_date di tbl_order_header
        var order_date = pool.StartTime;

        // ambil parameter untuk order dari api_mst_trip
        sql = @"SELECT TripId, CodeCustomer, Capacity, Balance, TrxID
                FROM api_mst_trip WITH(NOLOCK)
                WHERE RunID = @runid";
        var get_trip_for_payload = await conn_vrp.QueryAsync<ApiMstTrip>(new CommandDefinition
        (
            sql, new { runid }, transaction: trx_vrp, cancellationToken: cancellationToken
        ));

        // ambil geofence & client
        var geofences = await _gpsb_service.GetGPSBGeofences(company_id, cancellationToken);
        var clients = await _gpsb_service.GetGPSBClients(company_id, cancellationToken);
        // generate payload
        var order_payload = get_trip_for_payload.Select(x =>
        {
            var client_id = clients.FirstOrDefault(y => y.code.Trim().ToLower() == x.CodeCustomer.Trim().ToLower())?.client_id ?? throw new CustomException($"Client dengan CodeCustomer: {x.CodeCustomer} tidak dapat ditemukan. (TripID: {x.TripId})", StatusCodes.Status404NotFound);

            var geo_id = geofences.FirstOrDefault(y => y.code.Trim().ToLower() == x.TripId.Trim().ToLower())?.id ?? throw new CustomException($"Geofence dengan code: {x.TripId} tidak dapat ditemukan. (TripID: {x.TripId})", StatusCodes.Status404NotFound);

            return new
            {
                order_date,
                client_id,
                geo_id,
                capacity = x.Capacity,
                balance = x.Balance,
                pl = x.TrxID
            };
        });
        // create order
        foreach (var p in order_payload)
        {
            await _gpsb_service.CreateOrder
            (
                company_id: company_id,
                order_date: order_date,
                client_id: p.client_id,
                geo_id: p.geo_id,
                capacity: p.capacity ?? 0,
                balance: p.balance ?? 0,
                pl: p.pl ?? string.Empty,
                current_datetime: current_datetime,
                conn: conn_gpsb,
                trx: trx_gpsb,
                cancellationToken: cancellationToken
            );
        }
    }

    private static async Task<ApiMstPool> GetPool
    (
        string runid,
        DbConnection conn,
        DbTransaction trx,
        CancellationToken cancellationToken
    )
    {
        var sql = @"SELECT TOP 1 PoolID, PoolName, StartTime, StartLong, StartLat 
                    FROM api_mst_pool WITH(NOLOCK)
                    WHERE RunID = @runid";
        var pool = await conn.QueryFirstOrDefaultAsync<ApiMstPool>(new CommandDefinition
        (
            sql, new { runid }, transaction: trx, cancellationToken: cancellationToken
        )) ?? throw new CustomException("Data Pool tidak dapat ditemukan", StatusCodes.Status404NotFound);
        return pool;
    }

    private static async Task<List<ApiMstTrip>> GetTrips
    (
        string runid,
        DbConnection conn,
        DbTransaction trx,
        CancellationToken cancellationToken
    )
    {
        const string sql = @"SELECT RunID, TripID, TripName, TripLong, TripLat, TimeOpen, TimeClose,
                                    TimeWait, TimeOperation, Capacity, Balance, LayananID, TripType,
                                    MetodeHitung, Siklus, TrxID, ZoneCode, RegionCode, CodeCustomer
                             FROM api_mst_trip WITH(NOLOCK)
                             WHERE runid = @runid";
        var trips = await conn.QueryAsync<ApiMstTrip>(new CommandDefinition
        (
            sql, new { runid }, transaction: trx, cancellationToken: cancellationToken
        ));

        return [.. trips];
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
