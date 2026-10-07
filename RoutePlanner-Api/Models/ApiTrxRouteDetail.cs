using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

public class ApiTrxRouteDetail
{
    [JsonPropertyName("run_id")]
    public string? RunID { get; set; }

    [JsonPropertyName("route_no")]
    public int RouteNo { get; set; }

    [JsonPropertyName("seq")]
    public int Seq { get; set; }
    
    [JsonPropertyName("lon")]
    public double lon { get; set; }
    
    [JsonPropertyName("lat")]
    public double lat { get; set; }
}
