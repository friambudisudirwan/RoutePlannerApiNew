using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Services;

public class AuthService(
    ILogger<AuthService> logger,
    IHttpContextAccessor httpContext,
    IConfiguration config,
    VRPConnectionFactory vrp
)
{
    private readonly ILogger<AuthService> _logger = logger;
    private readonly IHttpContextAccessor _httpContext = httpContext;
    private readonly dynamic _jwtConfig = config.GetSection("JwtSettings");
    private readonly VRPConnectionFactory _vrp = vrp;

    public async Task<(bool result, string message, ConfMstUser? user)> AuthenticateAsync
    (
        string UserID,
        string Password,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var conn = _vrp.CreateConnection();
            if (conn.State == ConnectionState.Closed) await conn.OpenAsync(cancellationToken);

            var client_ip = _httpContext?.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            var p = new DynamicParameters();
            p.Add("@userid", UserID.Trim(), DbType.String, ParameterDirection.Input);
            p.Add("@password", Password.Trim(), DbType.String, ParameterDirection.Input);
            p.Add("@userip", client_ip.Trim(), DbType.String, ParameterDirection.Input);

            var cmd = new CommandDefinition("sp_app_login", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(cmd);

            var sql = @"SELECT UserID, CompanyID FROM conf_mst_user WITH(NOLOCK) WHERE UserID = @userid";
            var cmdUser = new CommandDefinition(sql, new { userid = UserID }, commandType: CommandType.Text, cancellationToken: cancellationToken);

            var user_attribute = await conn.QueryFirstOrDefaultAsync<ConfMstUser>(cmdUser);

            return (true, "Authentication success", user_attribute);
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Failed when authenticating user. {message}", ex.Message);
            return (false, ex.Message, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed when authenticating user. Internal server error.");
            throw;
        }
    }

    public async Task<(bool result, string message, string? token, ConfMstUser? user)> LoginAsync(string UserId, string Password, CancellationToken cancellationToken)
    {
        var authenticate = await AuthenticateAsync(UserId, Password, cancellationToken);
        if (!authenticate.result) return (false, authenticate.message, null, null);

        var token = GenerateToken(UserId, authenticate.user?.CompanyID.ToString() ?? "0");
        return (true, "Authentication Success", token, authenticate.user);
    }

    private string GenerateToken(string UserId, string CompanyID)
    {
        var claims = new List<Claim>()
        {
            new ("UserId", UserId),
            new ("CompanyId", CompanyID)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig["SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtConfig["Issuer"],
            audience: _jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(_jwtConfig["DurationInMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
