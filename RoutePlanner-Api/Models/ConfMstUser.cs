using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

/// <summary>User credentials / identity payload used by login and planner requests.</summary>
public class ConfMstUser
{
    /// <summary>User ID used for authentication.</summary>
    [JsonPropertyName("user_id")]
    public required string UserID { get; set; }

    /// <summary>User password used for authentication.</summary>
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    /// <summary>Optional display name (not required for login).</summary>
    public string? FullName { get; set; }

    /// <summary>Optional email (not required for login).</summary>
    public string? Email { get; set; }

    /// <summary>Optional company id (not required for login).</summary>
    public int? CompanyID { get; set; }
}
