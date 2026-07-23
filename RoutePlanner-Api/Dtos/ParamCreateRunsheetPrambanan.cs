using System;
using System.Text.Json.Serialization;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Dtos;

/// <summary>Request body for creating Prambanan runsheets.</summary>
public record ParamCreateRunsheetPrambanan
{
    /// <summary>Optional source system name.</summary>
    [JsonPropertyName("source_name")]
    public string? SourceName { get; set; }

    /// <summary>Planning start time.</summary>
    [JsonPropertyName("start_time")]
    public required DateTime StartTime { get; set; }

    /// <summary>
    /// Trip list. If any item has <c>car_plate</c>, manual routing is used; otherwise automatic planning.
    /// </summary>
    [JsonPropertyName("data")]
    public required List<ParamTripPrambanan> Data { get; set; }
}
