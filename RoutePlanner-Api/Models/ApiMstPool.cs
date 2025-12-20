using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

public class ApiMstPool
{
    public string? RunID { get; set; } = string.Empty;

    [JsonPropertyName("pool_id")]
    public required string PoolID { get; set; }

    [JsonPropertyName("pool_name")]
    public string PoolName { get; set; } = string.Empty;

    [JsonPropertyName("start_time")]
    public required DateTime StartTime { get; set; }

    [JsonPropertyName("lon")]
    public required string StartLong { get; set; }

    [JsonPropertyName("lat")]
    public required string StartLat { get; set; }

    [JsonPropertyName("max_time_idle")]
    public int MaxTimeIdle { get; set; }

    [JsonPropertyName("cars")]
    public List<ApiMstCar> Cars { get; set; } = [];
    
    [JsonPropertyName("trips")]
    public List<ApiMstTrip> Trips { get; set; } = [];
}
