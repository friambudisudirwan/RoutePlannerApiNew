using System.Data;
using System.Data.Common;
using Newtonsoft.Json;
using Dapper;
using RestSharp;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;

namespace RoutePlanner_Api.Services;

public class PrambananRunService
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
            var map_trips = param.Data.Where(x => /*IsInIndonesiaValid(x.TripLat.Trim(), x.TripLong.Trim())*/ IsValidLongLat(x.TripLong) && IsValidLongLat(x.TripLat)).Select(x => x with
            {
                TripLong = x.TripLong.Trim(),
                TripLat = x.TripLat.Trim(),
                IsValidLonLat = 1
            }).ToList();

            // ** list invalid trip
            map_trips.AddRange(param.Data.Where(x => /*!IsInIndonesiaValid(x.TripLat.Trim(), x.TripLong.Trim())*/!IsValidLongLat(x.TripLong) || !IsValidLongLat(x.TripLat)).Select((x, i) => x with
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
            // await PrerunPrambananPo
            // (
            //     list_runid,
            //     conn,
            //     cancellationToken
            // );

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
            _logger.LogError(ex, "Internal server error");
            throw;
        }
    }

    public async Task<List<long>> IntegrateRunsheets(ParamIntegrateRunsheets param, CancellationToken cancellationToken)
    {
        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

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
                var cmd_check = new CommandDefinition(sql, new { runid = run.RunId, carid = run.CarId, user_id }, commandType: CommandType.Text, cancellationToken: cancellationToken);
                var validate_route = await conn.QueryFirstOrDefaultAsync<string>(cmd_check);

                if (string.IsNullOrEmpty(validate_route)) throw new CreateRunsheetException("Route mobil tidak ditemukan.");

                // ** cek apakah route sudah terintegrasi
                sql = @"SELECT TOP 1 RunID FROM api_trx_route WITH(NOLOCK)
                        WHERE runid = @runid AND carid = @carid AND UsrUpd = @user_id AND ISNULL(IsPostDO, 0) = 1";
                var cmd_check2 = new CommandDefinition(sql, new { runid = run.RunId, carid = run.CarId, user_id }, commandType: CommandType.Text, cancellationToken: cancellationToken);
                var validate_route2 = await conn.QueryFirstOrDefaultAsync<string>(cmd_check2);

                if (!string.IsNullOrEmpty(validate_route2)) throw new CreateRunsheetException("Route mobil sudah pernah diintegrasikan ke TMS EasyGo.");

                // ** begin post do
                var p = new DynamicParameters();
                p.Add("@runid", run.RunId, DbType.String, ParameterDirection.Input);
                p.Add("@carid", run.CarId, DbType.String, ParameterDirection.Input);

                var cmd = new CommandDefinition("sp_posting_do_tms", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
                var fetch_do_post_param = await conn.QueryFirstOrDefaultAsync<string>(cmd) ?? throw new CreateRunsheetException("No data when preparing to integrate to TMS EasyGo. Internal server error");
                var do_post_param = JsonConvert.DeserializeObject<ParamCreateDoByGeoCode>(fetch_do_post_param);

                // ** update route to IsPostDo = 1
                sql = @"UPDATE api_trx_route SET IsPostDO = 1
                        WHERE RunId = @runid AND CarID = @carid";
                var cmd3 = new CommandDefinition(sql, new { runid = run.RunId, carid = run.CarId }, commandType: CommandType.Text, cancellationToken: cancellationToken);
                var update_route_ispostdo_status = await conn.ExecuteAsync(cmd3);

                // **hit vts api create do by code
                var client = new RestClient(_vtsApiUrl);
                var request = new RestRequest("/api/prambanan/AddOrUpdateDOV1ByGeoCode", Method.Post);

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

                if (!response.IsSuccessStatusCode) throw new InvalidOperationException(response.ErrorMessage);

                var responseData = JsonConvert.DeserializeObject<VtsApiResponseBase<DoIdData>>(response.Content ?? "") ?? throw new InvalidOperationException("Failed when integrating to TMS EasyGO");
                if (responseData.ResponseCode != 1) throw new InvalidOperationException(responseData.ResponseMessage);

                list_do_id.Add(responseData.Data?.do_id ?? 0);
            }

            // **commit trx
            return [.. list_do_id.Where(x => x > 0)];
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (CreateRunsheetException)
        {
            throw;
        }
        catch (Exception)
        {
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
        var sql = "DELETE FROM api_mst_trip WHERE runid = ''";
        var cmd_delete = new CommandDefinition(sql, cancellationToken: cancellationToken);
        await conn.ExecuteAsync(cmd_delete);

        var map_trips = trips.Select((x, i) => x with { SeqNo = i + 1, UsrUpd = UserId, DtmUpd = current_date_time });
        sql = @"INSERT INTO api_mst_trip (RunID, SeqNo, TripID, TripName, TripLong, TripLat, CityName,
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
