using System;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RoutePlanner_Api.Services;

public class ActionLogService
(
    IConfiguration config,
    ILogger<ActionLogService> logger,
    UserIdentityService userIdentity
)
{
    private readonly string _connectionstring = config.GetConnectionString("VRP") ?? throw new ArgumentNullException("Connection String VRP is empty.");
    private readonly ILogger<ActionLogService> _logger = logger;
    private readonly UserIdentityService _userIdentity = userIdentity;

    public async Task CreateLog
    (
        string runid,
        string type,
        string action_name,
        string log_body,
        string trace_id,
        CancellationToken cancellationToken
    )
    {
        var user_id = _userIdentity.GetUserId();
        var current_timestamp = DateTime.Now;
        using var conn = new SqlConnection(_connectionstring);

        const string sql = @"INSERT INTO action_logs (RunID, Type, ActionName, LogBody, TraceID, LogDate, Usrupd)
                             VALUES (@runid, @type, @action_name, @log_body, @trace_id, @current_timestamp, @user_id)";
        var cmd = new CommandDefinition(sql, new { runid, type, action_name, log_body, trace_id, current_timestamp, user_id }, cancellationToken: cancellationToken);

        await conn.ExecuteAsync(cmd);
    }
}
