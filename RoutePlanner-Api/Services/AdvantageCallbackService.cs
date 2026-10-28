using System.Data;
using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;

namespace RoutePlanner_Api.Services;

public sealed class AdvantageCallbackService
(
    VRPConnectionFactory vrp,
    AdvantageIntegrationService client,
    UserIdentityService userIdentity,
    ILogger<AdvantageCallbackService> logger
)
{
    private const string DateTimeFormat = "MM/dd/yyyy HH:mm:ss";

    private readonly VRPConnectionFactory _vrp = vrp;
    private readonly AdvantageIntegrationService _client = client;
    private readonly UserIdentityService _userIdentity = userIdentity;
    private readonly ILogger<AdvantageCallbackService> _logger = logger;

    public async Task EnsureSentAsync(string runid, int companyId, CancellationToken cancellationToken)
    {
        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

        if (await AlreadySentAsync(runid, conn, cancellationToken))
        {
            _logger.LogInformation("Advantage callback already sent for {RunId}, skip PUT", runid);
            return;
        }

        var payload = await BuildPayloadAsync(runid, conn, cancellationToken);
        var (httpStatus, message) = await _client.SendUpdateAsync(payload, cancellationToken);

        await InsertLogAsync(
            runid,
            companyId,
            httpStatus,
            message,
            payload.data.pool.totalRouted,
            payload.data.pool.totalUnrouted,
            conn,
            cancellationToken);
    }

    private static async Task<bool> AlreadySentAsync(string runid, IDbConnection conn, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT TOP 1 1 FROM api_log_advantage_callback WITH (NOLOCK) WHERE RunID = @runid";
        var exists = await conn.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { runid }, cancellationToken: cancellationToken));
        return exists is 1;
    }

    private async Task InsertLogAsync(
        string runid,
        int companyId,
        int httpStatus,
        string message,
        int totalRouted,
        int totalUnrouted,
        IDbConnection conn,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO api_log_advantage_callback
                (RunID, CompanyID, HttpStatus, Message, TotalRouted, TotalUnrouted, DtmCrt, UsrUpd)
            VALUES
                (@runid, @companyId, @httpStatus, @message, @totalRouted, @totalUnrouted, GETDATE(), @usrUpd)";

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    runid,
                    companyId,
                    httpStatus,
                    message,
                    totalRouted,
                    totalUnrouted,
                    usrUpd = _userIdentity.GetUserId()
                },
                cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            _logger.LogInformation("Advantage callback log already exists for {RunId}", runid);
        }
    }

    private static async Task<ParamAdvantageIntegration> BuildPayloadAsync(
        string runid,
        IDbConnection conn,
        CancellationToken cancellationToken)
    {
        const string sqlPool = @"
            SELECT TOP 1 RunID, PoolID, PoolName, StartTime, StartLong, StartLat, MaxTimeIdle
            FROM api_mst_pool WITH (NOLOCK)
            WHERE RunID = @runid";
        var pool = await conn.QueryFirstOrDefaultAsync<AdvantagePoolRow>(
            new CommandDefinition(sqlPool, new { runid }, cancellationToken: cancellationToken))
            ?? throw new CustomException("Data Pool tidak dapat ditemukan", StatusCodes.Status404NotFound);

        const string sqlSuccess = @"
            SELECT
                r.RunID,
                r.RouteNo,
                r.CarID,
                c.PoliceNo,
                r.CapacityStart,
                r.WorkingTimeStart,
                ISNULL(r.StartTrxID, '') AS StartTrxID,
                r.StartID,
                r.StartName,
                r.StartLong,
                r.StartLat,
                ISNULL(t.TrxID, '') AS EndTrxID,
                r.EndSeq,
                r.EndID,
                r.EndName,
                r.EndLong,
                r.EndLat,
                r.TimeOpen,
                r.TimeClose,
                r.MaxTimeIdle,
                r.StartTime,
                r.Duration,
                r.ArrivalTime,
                r.IdleTime,
                r.TimeWait,
                r.StartOperationTime,
                r.TimeOperation,
                r.TimeRest,
                r.EndTime,
                r.WorkingTimeEnd,
                r.CapacityUse,
                r.CapacityEnd,
                r.Distance,
                ISNULL(t.Balance, 0) AS Balance,
                ISNULL(t.LayananID, '') AS LayananID,
                ISNULL(t.TripType, '') AS TripType,
                ISNULL(t.MetodeHitung, '') AS MetodeHitung,
                ISNULL(t.Siklus, '') AS Siklus,
                ISNULL(t.TrxID, '') AS TrxID,
                ISNULL(t.ZoneCode, '') AS ZoneCode,
                ISNULL(t.RegionCode, '') AS RegionCode
            FROM api_trx_route r WITH (NOLOCK)
            LEFT JOIN api_mst_car c WITH (NOLOCK)
                ON r.RunID = c.RunID AND r.CarID = c.CarID
            LEFT JOIN api_mst_trip t WITH (NOLOCK)
                ON r.RunID = t.RunID AND r.EndID = t.TripID
            WHERE r.RunID = @runid
            ORDER BY r.RouteNo, r.EndSeq";

        var successRows = (await conn.QueryAsync<AdvantageSuccessRow>(
            new CommandDefinition(sqlSuccess, new { runid }, cancellationToken: cancellationToken))).ToList();

        const string sqlUnsuccess = @"
            SELECT
                t.TripID,
                t.TripName,
                t.TripLong,
                t.TripLat,
                t.TimeOpen,
                t.TimeClose,
                t.TimeWait,
                t.TimeOperation,
                t.Capacity,
                t.Balance,
                t.LayananID,
                t.ParentID,
                t.IsDv,
                t.UsrUpd,
                t.TripType,
                t.MetodeHitung,
                t.Siklus,
                t.TrxID,
                t.ZoneCode,
                t.RegionCode
            FROM api_mst_trip t WITH (NOLOCK)
            WHERE t.RunID = @runid
              AND NOT EXISTS (
                  SELECT 1
                  FROM api_trx_route r WITH (NOLOCK)
                  WHERE r.RunID = t.RunID AND r.EndID = t.TripID
              )";

        var unsuccessRows = (await conn.QueryAsync<AdvantageUnsuccessRow>(
            new CommandDefinition(sqlUnsuccess, new { runid }, cancellationToken: cancellationToken))).ToList();

        var routedTripCount = successRows
            .Where(x => !string.IsNullOrWhiteSpace(x.EndTrxID))
            .Select(x => x.EndID)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var totalUnrouted = unsuccessRows.Count;

        return new ParamAdvantageIntegration
        {
            statusCode = 200,
            message = "Success",
            data = new ParamAdvantageIntegrationData
            {
                pool = new ParamAdvantageIntegrationPool
                {
                    runID = pool.RunID,
                    poolID = pool.PoolID,
                    poolName = pool.PoolName,
                    startTime = FormatDate(pool.StartTime),
                    Long = pool.StartLong ?? "",
                    Lat = pool.StartLat ?? "",
                    maxTimeIdle = pool.MaxTimeIdle,
                    description = $"{pool.RunID} calculated completely.There are {routedTripCount} routed trips and {totalUnrouted} unrouted trips",
                    totalRouted = routedTripCount,
                    totalUnrouted = totalUnrouted
                },
                success = [.. successRows.Select(MapSuccess)],
                unsuccess = [.. unsuccessRows.Select(MapUnsuccess)]
            }
        };
    }

    private static ParamAdvantageIntegrationTripSuccess MapSuccess(AdvantageSuccessRow x) => new()
    {
        runID = x.RunID,
        routeNo = x.RouteNo,
        carID = x.CarID,
        policeNo = x.PoliceNo ?? "",
        capacityStart = x.CapacityStart,
        workingTimeStart = x.WorkingTimeStart,
        startTrxID = x.StartTrxID ?? "",
        startID = x.StartID ?? "",
        startName = x.StartName ?? "",
        startLong = x.StartLong ?? "",
        startLat = x.StartLat ?? "",
        endTrxID = x.EndTrxID ?? "",
        endSeq = x.EndSeq,
        endID = x.EndID ?? "",
        endName = x.EndName ?? "",
        endLong = x.EndLong ?? "",
        endLat = x.EndLat ?? "",
        timeOpen = FormatDate(x.TimeOpen),
        timeClose = FormatDate(x.TimeClose),
        maxTimeIdle = x.MaxTimeIdle,
        startTime = FormatDate(x.StartTime),
        duration = x.Duration,
        arrivalTime = FormatDate(x.ArrivalTime),
        idleTime = x.IdleTime,
        timeWait = x.TimeWait,
        startOperationTime = FormatDate(x.StartOperationTime),
        timeOperation = x.TimeOperation,
        timeRest = x.TimeRest,
        endTime = FormatDate(x.EndTime),
        workingTimeEnd = x.WorkingTimeEnd,
        capacityUse = x.CapacityUse,
        capacityEnd = x.CapacityEnd,
        distance = x.Distance,
        balance = x.Balance,
        layananID = x.LayananID ?? "",
        tripType = x.TripType ?? "",
        metodeHitung = x.MetodeHitung ?? "",
        siklus = x.Siklus ?? "",
        trxID = x.TrxID ?? "",
        zoneCode = x.ZoneCode ?? "",
        regionCode = x.RegionCode ?? ""
    };

    private static ParamAdvantageIntegrationTripUnsuccess MapUnsuccess(AdvantageUnsuccessRow x) => new()
    {
        tripID = x.TripID ?? "",
        tripName = x.TripName ?? "",
        tripLong = x.TripLong ?? "",
        tripLat = x.TripLat ?? "",
        timeOpen = FormatDate(x.TimeOpen),
        timeClose = FormatDate(x.TimeClose),
        timeWait = x.TimeWait,
        timeOperation = x.TimeOperation,
        capacity = x.Capacity,
        balance = x.Balance,
        layananID = x.LayananID ?? "",
        parentID = x.ParentID ?? "",
        isDv = x.IsDv,
        usrUpd = x.UsrUpd ?? "",
        tripType = x.TripType ?? "",
        metodeHitung = x.MetodeHitung ?? "",
        siklus = x.Siklus ?? "",
        trxID = x.TrxID ?? "",
        zoneCode = x.ZoneCode ?? "",
        regionCode = x.RegionCode ?? ""
    };

    private static string FormatDate(DateTime value)
    {
        if (value == DateTime.MinValue) return "";
        return value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
    }

    private sealed class AdvantagePoolRow
    {
        public string? RunID { get; set; }
        public string? PoolID { get; set; }
        public string? PoolName { get; set; }
        public DateTime StartTime { get; set; }
        public string? StartLong { get; set; }
        public string? StartLat { get; set; }
        public int MaxTimeIdle { get; set; }
    }

    private sealed class AdvantageSuccessRow
    {
        public string? RunID { get; set; }
        public int RouteNo { get; set; }
        public string? CarID { get; set; }
        public string? PoliceNo { get; set; }
        public double CapacityStart { get; set; }
        public int WorkingTimeStart { get; set; }
        public string? StartTrxID { get; set; }
        public string? StartID { get; set; }
        public string? StartName { get; set; }
        public string? StartLong { get; set; }
        public string? StartLat { get; set; }
        public string? EndTrxID { get; set; }
        public int EndSeq { get; set; }
        public string? EndID { get; set; }
        public string? EndName { get; set; }
        public string? EndLong { get; set; }
        public string? EndLat { get; set; }
        public DateTime TimeOpen { get; set; }
        public DateTime TimeClose { get; set; }
        public int MaxTimeIdle { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int IdleTime { get; set; }
        public int TimeWait { get; set; }
        public DateTime StartOperationTime { get; set; }
        public int TimeOperation { get; set; }
        public int TimeRest { get; set; }
        public DateTime EndTime { get; set; }
        public int WorkingTimeEnd { get; set; }
        public double CapacityUse { get; set; }
        public double CapacityEnd { get; set; }
        public double Distance { get; set; }
        public double Balance { get; set; }
        public string? LayananID { get; set; }
        public string? TripType { get; set; }
        public string? MetodeHitung { get; set; }
        public string? Siklus { get; set; }
        public string? TrxID { get; set; }
        public string? ZoneCode { get; set; }
        public string? RegionCode { get; set; }
    }

    private sealed class AdvantageUnsuccessRow
    {
        public string? TripID { get; set; }
        public string? TripName { get; set; }
        public string? TripLong { get; set; }
        public string? TripLat { get; set; }
        public DateTime TimeOpen { get; set; }
        public DateTime TimeClose { get; set; }
        public int TimeWait { get; set; }
        public int TimeOperation { get; set; }
        public double Capacity { get; set; }
        public double Balance { get; set; }
        public string? LayananID { get; set; }
        public string? ParentID { get; set; }
        public int IsDv { get; set; }
        public string? UsrUpd { get; set; }
        public string? TripType { get; set; }
        public string? MetodeHitung { get; set; }
        public string? Siklus { get; set; }
        public string? TrxID { get; set; }
        public string? ZoneCode { get; set; }
        public string? RegionCode { get; set; }
    }
}
