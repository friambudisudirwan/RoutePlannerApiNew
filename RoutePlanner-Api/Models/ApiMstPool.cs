using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

public record ApiMstPool
{
    [JsonPropertyName("run_id")]
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

    [JsonPropertyName("in_queue")]
    public int InQueue { get; set; }

    [JsonPropertyName("in_process")]
    public int InProcess { get; set; }

    [JsonPropertyName("is_failed")]
    public int IsFailed { get; set; }

    [JsonPropertyName("cars")]
    public List<ApiMstCar> Cars { get; set; } = [];
    
    [JsonPropertyName("trips")]
    public List<ApiMstTrip> Trips { get; set; } = [];

}
