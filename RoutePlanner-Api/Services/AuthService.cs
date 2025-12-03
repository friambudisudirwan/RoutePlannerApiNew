using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RoutePlanner_Api.Services;

public class AuthService(
    ILogger<AuthService> logger,
    IHttpContextAccessor httpContext,
    IConfiguration config
)
{
    // private readonly JwtSettings _jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new ArgumentNullException("JwtSettings is empty");
    private readonly ILogger<AuthService> _logger = logger;
    private readonly IHttpContextAccessor _httpContext = httpContext;
    private readonly string _connectionstringVRP = config.GetConnectionString("VRP") ?? throw new ArgumentNullException(nameof(config));

    public async Task<(bool result, string message)> AuthenticateAsync
    (
        string UserID,
        string Password,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var conn = new SqlConnection(_connectionstringVRP);
            if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

            var client_ip = _httpContext?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            var p = new DynamicParameters();
            p.Add("@userid", UserID.Trim(), DbType.String, ParameterDirection.Input);
            p.Add("@password", Password.Trim(), DbType.String, ParameterDirection.Input);
            p.Add("@userip", client_ip.Trim(), DbType.String, ParameterDirection.Input);

            var cmd = new CommandDefinition("sp_app_login", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(cmd);

            return (true, "Authentication success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed when authenticating user.");
            return (false, ex.Message);
        }
    }

    public async Task LoginAsync(string UserId, string Password, CancellationToken cancellationToken){

    }
}
