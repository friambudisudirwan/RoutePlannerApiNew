using System;
using System.Text.Json.Serialization;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Dtos;

public record ParamCreateRunsheetPrambanan
{
    [JsonPropertyName("start_time")]
    public required DateTime StartTime { get; set; }

    [JsonPropertyName("data")]
    public required List<ParamTripPrambanan> Data { get; set; }
}
