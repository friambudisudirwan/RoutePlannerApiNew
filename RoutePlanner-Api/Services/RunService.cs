using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Services;

public class RunService
(
    ILogger<RunService> logger,
    IConfiguration config
)
{
    private readonly ILogger<RunService> _logger = logger;
    private readonly string _connectionstringVRP = config.GetConnectionString("VRP") ?? throw new ArgumentNullException(nameof(config));

    public async Task<(int status, string message, List<string> data)> CreateRunsheets
    (
        ParamCreateRunsheets param,
        CancellationToken cancellationToken
    )
    {
        using var conn = new SqlConnection(_connectionstringVRP);
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);
        using var trx = await conn.BeginTransactionAsync(cancellationToken);

        // ** status 0 = initial, 500 = error, 200 = success 
        var status = 0;
        var message = "";

        var created_runid = new List<string>();

        try
        {
            // ** begin create runsheets
            foreach (var pool in param.Data)
            {
                var runid = await GetRunID(conn, trx, cancellationToken);

                var pPool = new DynamicParameters();
                pPool.Add("@runid", runid, DbType.String, ParameterDirection.Input);
                pPool.Add("@poolid", pool.PoolID, DbType.String, ParameterDirection.Input);
                pPool.Add("@poolname", pool.PoolName, DbType.String, ParameterDirection.Input);
                pPool.Add("@starttime", pool.StartTime, DbType.String, ParameterDirection.Input);
                pPool.Add("@startlong", pool.StartLong, DbType.String, ParameterDirection.Input);
                pPool.Add("@startlat", pool.StartLat, DbType.String, ParameterDirection.Input);
                pPool.Add("@maxtimeidle", pool.MaxTimeIdle, DbType.Int32, ParameterDirection.Input);
                pPool.Add("@usrupd", param.User.UserID, DbType.String, ParameterDirection.Input);

                var cmdPool = new CommandDefinition("sp_api_run_insert_pool", pPool, commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);

                if (await conn.ExecuteAsync(cmdPool) < 1) throw new InvalidOperationException($"Failed when creating pool for PoolID: {pool.PoolID}. No row affected. Internal server error.");

                // ** insert cars

                // ** insert trips

            }

            status = 200;
            message = "Runsheets created";
            await trx.CommitAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await trx.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Failed when creating runsheet.");
            status = 500;
            message = ex.Message;
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Internal server error while creating runsheet.");
            status = 500;
            message = ex.Message;
        }
        finally
        {
            await trx.DisposeAsync();
        }

        return (status, message, created_runid);
    }

    private async Task InsertCars
    (
        List<ApiMstCar> cars,
        SqlConnection conn,
        DbTransaction trx,
        CancellationToken cancellationToken
    )
    {

    }
    private async Task InsertTrips
    (
        List<ApiMstTrip> trips,
        SqlConnection conn,
        DbTransaction trx,
        CancellationToken cancellationToken
    )
    {

    }

    private static async Task<string> GetRunID(SqlConnection conn, DbTransaction trx, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition("sp_get_runid", parameters: null, commandType: CommandType.StoredProcedure, transaction: trx, cancellationToken: cancellationToken);

        var runid = await conn.QueryFirstOrDefaultAsync<string>(cmd) ?? throw new InvalidOperationException("Failed when getting RunID from database. Internal server error.");
        return runid;
    }
}
