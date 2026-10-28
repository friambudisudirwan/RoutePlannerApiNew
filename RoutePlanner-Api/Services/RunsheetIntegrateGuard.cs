using System.Data;
using Dapper;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Exceptions;

namespace RoutePlanner_Api.Services;

public sealed class RunsheetIntegrateGuard(VRPConnectionFactory vrp)
{
    private readonly VRPConnectionFactory _vrp = vrp;

    public async Task EnsureReadyAsync(string runid, CancellationToken cancellationToken)
    {
        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

        const string sqlPool = @"SELECT TOP 1 InQueue, InProcess, IsFailed
                                  FROM api_mst_pool WITH (NOLOCK)
                                  WHERE RunID = @runid";
        var pool = await conn.QueryFirstOrDefaultAsync<PoolReadyRow>(
            new CommandDefinition(sqlPool, new { runid }, cancellationToken: cancellationToken));

        if (pool is null)
            throw new CustomException("Data Pool tidak dapat ditemukan", StatusCodes.Status404NotFound);

        if (pool.InQueue == 1)
            throw new CustomException(
                "Runsheet is still in queue. Integration can be done after routing is complete.",
                StatusCodes.Status422UnprocessableEntity);

        if (pool.InProcess == 1)
            throw new CustomException(
                "Runsheet is still being calculated. Please wait until routing is complete.",
                StatusCodes.Status422UnprocessableEntity);

        if (pool.IsFailed == 1)
            throw new CustomException(
                "Routing failed. This runsheet cannot be integrated.",
                StatusCodes.Status422UnprocessableEntity);

        if (pool.InProcess != 2)
            throw new CustomException(
                "Runsheet has not finished routing.",
                StatusCodes.Status422UnprocessableEntity);

        const string sqlCounts = @"
            SELECT
                (SELECT COUNT(*) FROM api_mst_trip WITH (NOLOCK) WHERE RunID = @runid) AS TotalTrip,
                (
                    SELECT COUNT(*)
                    FROM api_mst_trip t WITH (NOLOCK)
                    WHERE t.RunID = @runid
                      AND NOT EXISTS (
                          SELECT 1
                          FROM api_trx_route r WITH (NOLOCK)
                          WHERE r.RunID = t.RunID AND r.EndSeq = t.SeqNo
                      )
                ) AS Unrouted";

        var counts = await conn.QueryFirstAsync<TripCountRow>(
            new CommandDefinition(sqlCounts, new { runid }, cancellationToken: cancellationToken));

        if (counts.TotalTrip < 1)
            throw new CustomException(
                "This runsheet does not have any trip.",
                StatusCodes.Status422UnprocessableEntity);

        if (counts.Unrouted > 0)
        {
            var routed = counts.TotalTrip - counts.Unrouted;
            throw new CustomException(
                $"Not all trips are routed ({routed} of {counts.TotalTrip}). Integration is only allowed when every trip is routed.",
                StatusCodes.Status422UnprocessableEntity);
        }
    }

    private sealed class PoolReadyRow
    {
        public int InQueue { get; set; }
        public int InProcess { get; set; }
        public int IsFailed { get; set; }
    }

    private sealed class TripCountRow
    {
        public int TotalTrip { get; set; }
        public int Unrouted { get; set; }
    }
}
