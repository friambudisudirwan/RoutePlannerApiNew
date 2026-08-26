using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

public record ApiTrxRoute
{
    [JsonPropertyName("run_id")]
    public string? RunID { get; set; }

    [JsonPropertyName("route_no")]
    public int RouteNo { get; set; }

    [JsonPropertyName("car_id")]
    public string? CarID { get; set; }

    [JsonPropertyName("capacity_start")]
    public double CapacityStart { get; set; }

    [JsonPropertyName("working_time_start")]
    public int WorkingTimeStart { get; set; }

    [JsonPropertyName("start_id")]
    public string? StartID { get; set; }

    [JsonPropertyName("start_name")]
    public string? StartName { get; set; }

    [JsonPropertyName("start_long")]
    public string? StartLong { get; set; }

    [JsonPropertyName("start_lat")]
    public string? StartLat { get; set; }

    [JsonPropertyName("end_seq")]
    public int EndSeq { get; set; }

    [JsonPropertyName("end_id")]
    public string? EndID { get; set; }

    [JsonPropertyName("end_name")]
    public string? EndName { get; set; }

    [JsonPropertyName("end_long")]
    public string? EndLong { get; set; }

    [JsonPropertyName("end_lat")]
    public string? EndLat { get; set; }

    [JsonPropertyName("time_open")]
    public DateTime TimeOpen { get; set; }

    [JsonPropertyName("time_close")]
    public DateTime TimeClose { get; set; }

    [JsonPropertyName("max_time_idle")]
    public int MaxTimeIdle { get; set; }

    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("arrival_time")]
    public DateTime ArrivalTime { get; set; }

    [JsonPropertyName("idle_time")]
    public int IdleTime { get; set; }

    [JsonPropertyName("time_wait")]
    public int TimeWait { get; set; }

    [JsonPropertyName("start_operation_time")]
    public DateTime StartOperationTime { get; set; }

    [JsonPropertyName("time_operation")]
    public int TimeOperation { get; set; }

    [JsonPropertyName("time_rest")]
    public int TimeRest { get; set; }

    [JsonPropertyName("end_time")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("working_time_end")]
    public int WorkingTimeEnd { get; set; }

    [JsonPropertyName("capacity_use")]
    public double CapacityUse { get; set; }

    [JsonPropertyName("capacity_end")]
    public double CapacityEnd { get; set; }

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("route_details")]
    public List<ApiTrxRouteDetail> RouteDetails { get; set; } = [];
}
