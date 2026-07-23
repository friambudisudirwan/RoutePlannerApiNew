using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Dtos;

public class ParamAdvantageIntegrationPool
{
    public string? runID { get; set; }
    public string? poolID { get; set; }
    public string? poolName { get; set; }
    public string? startTime { get; set; }
    [JsonPropertyName("long")]
    public string? Long { get; set; }
    [JsonPropertyName("lat")]
    public string? Lat { get; set; }
    public int maxTimeIdle { get; set; }
    public string? description { get; set; }
    public int totalRouted { get; set; }
    public int totalUnrouted { get; set; }
}
