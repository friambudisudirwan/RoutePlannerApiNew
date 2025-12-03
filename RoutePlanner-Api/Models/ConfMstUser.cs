using System;

namespace RoutePlanner_Api.Models;

public class ConfMstUser
{
    public required string UserID { get; set; }
    public required string Password { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
}
