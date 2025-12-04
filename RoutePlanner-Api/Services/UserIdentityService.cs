using System;
using System.Security.Claims;

namespace RoutePlanner_Api.Services;

public class UserIdentityService(IHttpContextAccessor httpContextAccessor)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string? GetUserId()
    {
        return _httpContextAccessor?.HttpContext?.User?.FindFirst("UserId")?.Value;
    }

    public int GetCompanyId()
    {
        return Convert.ToInt32(_httpContextAccessor?.HttpContext?.User?.FindFirst("UserId")?.Value);
    }
}
