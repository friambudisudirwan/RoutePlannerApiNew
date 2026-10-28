using System.Data;
using System.Data.Common;
using Newtonsoft.Json;
using Dapper;
using RestSharp;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Validator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace RoutePlanner_Api.Services;

public class PrambananRunService
(
    IConfiguration config,
    ILogger<RunService> logger,
    IBrokerService brokerService,
    VRPConnectionFactory vrp,
    GPSBConnectionFactory gpsb,
    PrambananValidator validator,
    UserIdentityService userIdentity,
    RunsheetIntegrateGuard integrateGuard
)
{
    private readonly ILogger<RunService> _logger = logger;
    private readonly VRPConnectionFactory _vrp = vrp;
    private readonly GPSBConnectionFactory _gpsb = gpsb;
    private readonly IBrokerService _brokerServie = brokerService;
    private readonly dynamic _brokerConfig = config.GetSection("RabbitMQConfig");
    private readonly UserIdentityService _userIdentity = userIdentity;
    private readonly PrambananValidator _validator = validator;
    private readonly RunsheetIntegrateGuard _integrateGuard = integrateGuard;
    private readonly string _vtsApiUrl = config.GetSection("Configs")["VtsApiUrl"] ?? throw new ArgumentNullException("Vts Api Url is empty");

    public async Task<List<string>> CreatePrambananManualRunsheets(ParamCreateRunsheetPrambanan param, CancellationToken cancellationToken)
    {
        var company_id = _userIdentity.GetCompanyId();
        var user_id = _userIdentity.GetUserId();
        var current_date_time = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        if (param.StartTime == DateTime.MinValue) throw new PrambananSoValidationException("start_time format is not a valid date format.", [], []);

        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

        try
        {
            var validate = _validator.ValidatePrambananSo(param.Data);
            if (!validate.result) throw new PrambananSoValidationException("Bad Request", validate.list_duplicate_so, validate.list_not_valid_lon_lat);

            // ** insert trips
            await InsertPrambananTrips
            (
                current_date_time,
                user_id ?? "",
                validate.list_so,
                conn,
                cancellationToken
            );

            // ** pre run inserted trips
            var list_runid = await PrerunPrambananTripsManual
            (
                company_id: company_id,
                user_id: user_id ?? "",
                current_date_time: current_date_time,
                start_time: param.StartTime,
                source_name: param.SourceName ?? "Prambanan Web Upload Manual",
                conn: conn,
                cancellationToken: cancellationToken
            );

            foreach (var runid in list_runid)
            {
                var cmd_queue = new CommandDefinition(@"UPDATE api_mst_pool SET InQueue = 1 WHERE RunID = @runid", new { runid }, cancellationToken: cancellationToken);
                await conn.ExecuteAsync(cmd_queue);
            }

            if (list_runid.Count > 0)
            {
                string? manualRoutingKey = _brokerConfig["ManualRoutingKey"];
                if (string.IsNullOrWhiteSpace(manualRoutingKey))
                    throw new InvalidOperationException("RabbitMQConfig:ManualRoutingKey is not configured");

                await _brokerServie.PublishMessage
                (
                    exchange: _brokerConfig["ExchangeName"],
                    routing_key: manualRoutingKey,
                    message: JsonConvert.SerializeObject(new
                    {
                        userid = user_id,
                        dtmupd = current_date_time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        runids = list_runid
                    })
                );
            }

            return list_runid;
        }
        catch (Exception ex)
        {
            // ** delete apabila ada so yang nggak dapet runid (meskipun ngga mungkin)
            var sql = "DELETE FROM api_mst_trip WHERE runid = '' AND usrupd = @user_id AND dtmupd = @current_date_time";
            var cmd_delete = new CommandDefinition(sql, new { user_id, current_date_time }, cancellationToken: cancellationToken, commandTimeout: 60 * 5);
            await conn.ExecuteAsync(cmd_delete);

            _logger.LogError(ex, "Internal server error");
            throw;
        }
    }
    public async Task<List<string>> CreatePrambananRunsheets(ParamCreateRunsheetPrambanan param, CancellationToken cancellationToken)
    {
        var company_id = _userIdentity.GetCompanyId();
        var user_id = _userIdentity.GetUserId();
        var current_date_time = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        if (param.StartTime == DateTime.MinValue) throw new PrambananSoValidationException("start_time format is not a valid date format.", [], []);

        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

        try
        {
            var validate = _validator.ValidatePrambananSo(param.Data);
            if (!validate.result) throw new PrambananSoValidationException("Bad Request", validate.list_duplicate_so, validate.list_not_valid_lon_lat);

            // ** insert trips
            await InsertPrambananTrips
            (
                current_date_time,
                user_id ?? "",
                validate.list_so,
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
                param.SourceName ?? "API Integration",
                conn,
                cancellationToken
            );

            // ** delete apabila ada so yang nggak dapet runid (meskipun ngga mungkin)
            await DeleteTripWithNoRunIDByTime(user_id ?? "", current_date_time, conn, cancellationToken);

            foreach (var runid in list_runid)
            {
                var cmd_queue = new CommandDefinition(@"UPDATE api_mst_pool SET InQueue = 1 WHERE RunID = @runid", new { runid }, cancellationToken: cancellationToken);
                await conn.ExecuteAsync(cmd_queue);
            }

            // ** hit broker rabbitmq buat jalanin background service
            await _brokerServie.PublishMessage
            (
                exchange: _brokerConfig["ExchangeName"],
                routing_key: _brokerConfig["RoutingKey"],
                message: JsonConvert.SerializeObject(list_runid.GroupBy(x => x).Select(x => new
                {
                    runid = x.Key,
                    userid = user_id,
                    start_time = param.StartTime,
                    company_id
                }))
            );

            return list_runid;
        }
        catch (Exception ex)
        {
            await DeleteTripWithNoRunIDByTime(user_id ?? "", current_date_time, conn, cancellationToken);
            _logger.LogError(ex, "Internal server error");
            throw;
        }
    }

    public async Task<IntegrateRunsheetsResult> IntegrateRunsheets(ParamIntegrateRunsheets param, CancellationToken cancellationToken)
    {
        if (param.data is null || param.data.Count == 0)
            throw new CustomException("Tidak ada data kendaraan untuk diintegrasikan.", StatusCodes.Status422UnprocessableEntity);

        using var conn = _vrp.CreateConnection();
        using var conn_gpsb = await _gpsb.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);
        if (conn_gpsb.State == ConnectionState.Closed) await conn_gpsb.OpenAsync(cancellationToken);

        _logger.LogInformation("Param received at {time}, param : {param}", DateTime.Now, JsonConvert.SerializeObject(param));

        var company_id = _userIdentity.GetCompanyId();
        var user_id = _userIdentity.GetUserId();
        var token_h2h = await _userIdentity.GetTokenH2H(cancellationToken);

        foreach (var runid in param.data.Select(x => x.RunId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            await _integrateGuard.EnsureReadyAsync(runid, cancellationToken);

        var result = new IntegrateRunsheetsResult();

        foreach (var run in param.data)
        {
            try
            {
                var do_id = await IntegrateVehicleAsync(
                    company_id,
                    user_id,
                    token_h2h,
                    run,
                    conn,
                    conn_gpsb,
                    cancellationToken);
                if (do_id > 0) result.DoIds.Add(do_id);
                result.SuccessCarIds.Add(run.CarId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed integrating Prambanan {RunId} / {CarId}", run.RunId, run.CarId);
                result.Errors.Add(new IntegrateCarErrorItem
                {
                    CarId = run.CarId,
                    Message = string.IsNullOrWhiteSpace(ex.Message) ? "Internal server error" : ex.Message
                });
            }
        }

        return result;
    }

    private async Task<long> IntegrateVehicleAsync
    (
        int company_id,
        string user_id,
        string token_h2h,
        ParamTrxRoute run,
        DbConnection conn,
        DbConnection conn_gpsb,
        CancellationToken cancellationToken
    )
    {
        var sql = @"SELECT TOP 1 RunID FROM api_trx_route WITH(NOLOCK)
                    WHERE runid = @runid AND carid = @carid AND UsrUpd = @user_id";
        var validate_route = await conn.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            sql, new { runid = run.RunId, carid = run.CarId, user_id }, cancellationToken: cancellationToken));

        if (string.IsNullOrEmpty(validate_route))
            throw new CreateRunsheetException("Route mobil tidak ditemukan.");

        sql = @"SELECT TOP 1 RunID FROM api_trx_route WITH(NOLOCK)
                WHERE runid = @runid AND carid = @carid AND UsrUpd = @user_id AND ISNULL(IsPostDO, 0) = 1";
        var validate_route2 = await conn.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            sql, new { runid = run.RunId, carid = run.CarId, user_id }, cancellationToken: cancellationToken));

        if (!string.IsNullOrEmpty(validate_route2))
            throw new CreateRunsheetException("Route mobil sudah pernah diintegrasikan ke TMS EasyGo.");

        var p = new DynamicParameters();
        p.Add("@runid", run.RunId, DbType.String, ParameterDirection.Input);
        p.Add("@carid", run.CarId, DbType.String, ParameterDirection.Input);

        var cmd = new CommandDefinition("sp_posting_do_tms", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken, commandTimeout: 60 * 30);
        var fetch_do_post_param = await conn.QueryFirstOrDefaultAsync<string>(cmd)
            ?? throw new CreateRunsheetException("No data when preparing to integrate to TMS EasyGo. Internal server error");
        var do_post_param = JsonConvert.DeserializeObject<ParamCreateDoByGeoCode>(fetch_do_post_param);
        if (do_post_param.shipment is not null)
        {
            do_post_param.alert_email = "transport1.ndc@prb.co.id;transport2.ndc@prb.co.id;transport3.ndc@prb.co.id;transport4.ndc@prb.co.id;transport1.edc@prb.co.id;transport2.edc@prb.co.id;transport3.3dc@prb.co.id;saefudin@prb.co.id;j.prasetyo@prb.co.id;s.arifin@prb.co.id";
        }

        var client = new RestClient(_vtsApiUrl);
        var request = new RestRequest("/api/prambanan/AddOrUpdateDOV1ByGeoCode", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Token", token_h2h);
        request.AddParameter("application/json", JsonConvert.SerializeObject(do_post_param), ParameterType.RequestBody);

        var response = await client.ExecuteAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(response.ErrorMessage);

        var responseData = JsonConvert.DeserializeObject<VtsApiResponseBase<DoIdData>>(response.Content ?? "")
            ?? throw new InvalidOperationException("Failed when integrating to TMS EasyGO");
        if (responseData.ResponseCode != 1)
            throw new CreateRunsheetException(responseData?.ResponseMessage ?? "");

        sql = @"UPDATE api_trx_route SET IsPostDO = 1
                WHERE RunId = @runid AND CarID = @carid";
        await conn.ExecuteAsync(new CommandDefinition(
            sql, new { runid = run.RunId, carid = run.CarId }, cancellationToken: cancellationToken));

        sql = @"SELECT TrxID, PL, PS FROM api_mst_trip WITH(NOLOCK)
                WHERE RunID = @runid";
        var fetch_data_so = await conn.QueryAsync(new CommandDefinition(sql, new { runid = run.RunId }, cancellationToken: cancellationToken));

        foreach (var so in fetch_data_so)
        {
            sql = @"UPDATE tbl_order_header SET is_do = 1
                    WHERE company_id = @company_id AND order_no = @order_no AND pl = @pl AND ps = @ps";
            await conn_gpsb.ExecuteAsync(new CommandDefinition(
                sql,
                new { company_id, order_no = (string)so.TrxID, pl = (string)so.pl, ps = (string)so.ps },
                cancellationToken: cancellationToken));
        }

        return responseData.Data?.do_id ?? 0;
    }

    public async Task UpdatePS(ParamUpdatePS param, CancellationToken cancellationToken)
    {
        var list_not_found_so = new List<ParamUpdatePSItem>();
        var company_id = _userIdentity.GetCompanyId();

        using (var conn = await _gpsb.CreateConnection())
        {
            if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

            const string sqlCheck = """
                SELECT TOP 1 order_id
                FROM tbl_order_header WITH (NOLOCK)
                WHERE company_id = @company_id AND order_no = @so_no AND pl = @pl AND is_enabled = 1
                """;

            foreach (var row in param.Data)
            {
                var cmd = new CommandDefinition(sqlCheck, new { company_id, so_no = row.SoNo, pl = row.Pl }, cancellationToken: cancellationToken);
                var order_id = await conn.QueryFirstOrDefaultAsync<long?>(cmd);
                if (order_id is null or 0)
                    list_not_found_so.Add(row);
            }
        }

        if (list_not_found_so.Count > 0)
            throw new UpdatePSNotFoundException(list_not_found_so);

        const string sqlTrip = """
            UPDATE api_mst_trip SET ps = @ps
            WHERE TrxID = @so_no AND PL = @pl AND isdeleted = 0
            """;

        const string sqlOrderHeader = """
            UPDATE tbl_order_header SET ps = @ps
            WHERE company_id = @company_id AND order_no = @so_no AND pl = @pl AND is_enabled = 1
            """;

        const string sqlDoHeader = """
            UPDATE a SET a.ps = @ps
            FROM tbl_do_v1_tujuan a
            INNER JOIN tbl_do_v1_header b ON a.do_id = b.do_id
            WHERE b.company_id = @company_id AND a.no_sj = @so_no and a.pl = @pl
            """;

        using var connVrp = _vrp.CreateConnection();
        if (connVrp.State == ConnectionState.Closed) await connVrp.OpenAsync(cancellationToken);
        using var trxVrp = await connVrp.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var row in param.Data)
            {
                var cmdTrip = new CommandDefinition(sqlTrip, new { ps = row.Ps, so_no = row.SoNo, pl = row.Pl }, transaction: trxVrp, cancellationToken: cancellationToken);
                await connVrp.ExecuteAsync(cmdTrip);
            }

            await trxVrp.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await trxVrp.RollbackAsync(cancellationToken);
            throw;
        }

        using var connGpsbUpd = await _gpsb.CreateConnection();
        if (connGpsbUpd.State == ConnectionState.Closed) await connGpsbUpd.OpenAsync(cancellationToken);
        using var trxGpsb = await connGpsbUpd.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var row in param.Data)
            {
                var cmdHdr = new CommandDefinition(sqlOrderHeader, new { ps = row.Ps, company_id, so_no = row.SoNo, pl = row.Pl }, transaction: trxGpsb, cancellationToken: cancellationToken);
                await connGpsbUpd.ExecuteAsync(cmdHdr);

                var cmdDo = new CommandDefinition(sqlDoHeader, new { company_id, so_no = row.SoNo, pl = row.Pl, ps = row.Ps }, transaction: trxGpsb, cancellationToken: cancellationToken);
                await connGpsbUpd.ExecuteAsync(cmdDo);
            }

            await trxGpsb.CommitAsync(cancellationToken);
        }
        catch
        {
            await trxGpsb.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task DeleteTripWithNoRunIDByTime(string user_id, DateTime dtmupd, DbConnection conn, CancellationToken cancellationToken)
    {
        // ** delete apabila ada so yang nggak dapet runid (meskipun ngga mungkin)
        var sql = "DELETE FROM api_mst_trip WHERE runid = '' AND usrupd = @user_id AND dtmupd = @dtmupd";
        var cmd_delete = new CommandDefinition(sql, new { user_id, dtmupd }, cancellationToken: cancellationToken, commandTimeout: 60 * 30);
        await conn.ExecuteAsync(cmd_delete);
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

            var cmd = new CommandDefinition("sp_prerun_prambanan_po", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken, commandTimeout: 60 * 30);
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
        var sql = @"INSERT INTO api_mst_trip (CarIDManual, SeqNoManual, RunID, SeqNo, TripID, TripName, TripAddress, TripLong, TripLat, CityName,
                                              Capacity, Balance, TrxID, Warehouse, BU, PL, PS, StorageType, 
                                              NoSo, CodeCustomer, Segment, TotalQty, TotalGrossVolume, IsAllowRoute,
                                              IsValidLonLat, UsrUpd, DtmUpd, source_data)
                    VALUES (@policeno, @seqnomanual, '', @seqno, @tripid, @tripname, @address, @triplong, @triplat, @cityname,
                            @capacity, @balance, @trxid, @poolid, @bu, @pl, @ps, @storagetype, 
                            @noso, @codecustomer, @segment, @totalqty, @totalgrossvolume, 1,
                            @isvalidlonlat, @usrupd, @dtmupd, 'Api-Prambanan')";

        var cmd = new CommandDefinition(sql, map_trips, commandType: CommandType.Text, cancellationToken: cancellationToken, commandTimeout: 60 * 30);
        await conn.ExecuteAsync(cmd);
    }

    private static async Task<List<string>> PrerunPrambananTripsManual
    (
        int company_id,
        string user_id,
        DateTime current_date_time,
        DateTime start_time,
        string source_name,
        DbConnection conn,
        CancellationToken cancellationToken
    )
    {
        var p = new DynamicParameters();
        p.Add("@company_id", company_id, DbType.Int32, ParameterDirection.Input);
        p.Add("@usrupd", user_id, DbType.String, ParameterDirection.Input);
        p.Add("@dtmupd", current_date_time, DbType.DateTime, ParameterDirection.Input);
        p.Add("@start_time", start_time, DbType.DateTime, ParameterDirection.Input);
        p.Add("@source_name", source_name, DbType.String, ParameterDirection.Input);

        var cmd_prerun = new CommandDefinition("sp_prerun_prambanan_manual", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken, commandTimeout: 60 * 60);
        await conn.ExecuteAsync(cmd_prerun);

        var sql = @"SELECT runid FROM api_mst_trip WITH(NOLOCK)
                    WHERE usrupd = @user_id AND dtmupd = @current_date_time AND runid != ''
                    GROUP BY runid";
        var cmd2 = new CommandDefinition(sql, new { user_id, current_date_time }, commandType: CommandType.Text, cancellationToken: cancellationToken, commandTimeout: 60 * 60);
        var list_runid = await conn.QueryAsync<string>(cmd2);

        return [.. list_runid];
    }

    private static async Task<List<string>> PrerunPrambananTrips
    (
        int company_id,
        string user_id,
        DateTime start_time,
        DateTime current_date_time,
        string source_name,
        DbConnection conn,
        CancellationToken cancellationToken
    )
    {
        var p = new DynamicParameters();
        p.Add("@company_id", company_id, DbType.Int32, ParameterDirection.Input);
        p.Add("@usrupd", user_id, DbType.String, ParameterDirection.Input);
        p.Add("@dtmupd", current_date_time, DbType.DateTime, ParameterDirection.Input);
        p.Add("@start_time", start_time, DbType.DateTime, ParameterDirection.Input);
        p.Add("@source_name", source_name, DbType.String, ParameterDirection.Input);

        var cmd = new CommandDefinition("sp_prerun_prambanan_auto", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken, commandTimeout: 60 * 60);
        await conn.ExecuteAsync(cmd);

        var sql = @"SELECT runid FROM api_mst_trip WITH(NOLOCK)
                    WHERE usrupd = @user_id AND dtmupd = @current_date_time AND runid != ''
                    GROUP BY runid";
        var cmd2 = new CommandDefinition(sql, new { user_id, current_date_time }, commandType: CommandType.Text, cancellationToken: cancellationToken, commandTimeout: 60 * 60);
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
