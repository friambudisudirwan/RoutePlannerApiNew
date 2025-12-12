using System;
using System.Security.Claims;
using RoutePlanner_Api.Data;
using System.Data;
using Dapper;

namespace RoutePlanner_Api.Services;

public class UserIdentityService
(
    IHttpContextAccessor httpContextAccessor,
    VRPConnectionFactory vrp
)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly VRPConnectionFactory _vrp = vrp;

    public string? GetUserId()
    {
        return _httpContextAccessor?.HttpContext?.User?.FindFirst("UserId")?.Value;
    }

    public int GetCompanyId()
    {
        return Convert.ToInt32(_httpContextAccessor?.HttpContext?.User?.FindFirst("CompanyId")?.Value);
    }

    public async Task<string> GetTokenH2H(CancellationToken cancellationToken)
    {
        var user_id = GetUserId();

        using var conn = _vrp.CreateConnection();
        if (conn.State == ConnectionState.Closed) await conn.OpenAsync();

        var sql = @"SELECT TokenH2H FROM conf_mst_user WHERE UserID = @user_id";
        var cmd = new CommandDefinition(sql, new { user_id }, commandType: CommandType.Text, cancellationToken: cancellationToken);

        return await conn.QueryFirstOrDefaultAsync<string>(cmd) ?? throw new InvalidOperationException("Token H2H is not configured for this user.");
    }
}
