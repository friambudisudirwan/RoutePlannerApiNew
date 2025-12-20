using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

public class ApiMstCar
{
    public string? RunID { get; set; } = string.Empty;
    public int? SeqNo { get; set; }

    [JsonPropertyName("car_id")]
    public required string CarID { get; set; }

    [JsonPropertyName("car_desc")]
    public string? CarDesc { get; set; } = string.Empty;

    [JsonPropertyName("police_no")]
    public string? PoliceNo { get; set; }

    [JsonPropertyName("capacity")]
    public double Capacity { get; set; }

    [JsonPropertyName("working_time")]
    public int? WorkingTime { get; set; }

    [JsonPropertyName("min_rest_time")]
    public string? MinRestTime { get; set; }

    [JsonPropertyName("rest_time")]
    public int? RestTime { get; set; }
}
