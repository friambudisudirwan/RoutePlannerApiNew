using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

public class ConfMstUser
{
    [JsonPropertyName("user_id")]
    public required string UserID { get; set; }

    [JsonPropertyName("password")]
    public required string Password { get; set; }
    
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public int? CompanyID { get; set; }
}
