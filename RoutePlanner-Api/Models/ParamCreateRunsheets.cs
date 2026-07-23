using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

/// <summary>Request body for creating generic planner runsheets.</summary>
public class ParamCreateRunsheets
{
    /// <summary>Optional source system name.</summary>
    [JsonPropertyName("source_name")]
    public string? SourceName { get; set; }

    /// <summary>User context for the planning request.</summary>
    [JsonPropertyName("user")]
    public required ConfMstUser User { get; set; }

    /// <summary>Pool list with cars and trips to plan.</summary>
    [JsonPropertyName("data")]
    public required List<ApiMstPool> Data { get; set; }
}
