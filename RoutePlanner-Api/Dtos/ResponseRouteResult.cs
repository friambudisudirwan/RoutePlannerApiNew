using System;
using System.Text.Json.Serialization;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Dtos;

public class ResponseRouteResult
{
    [JsonPropertyName("pool")]
    public required ApiMstPool Pool { get; set; }

    [JsonPropertyName("routes")]
    public List<ApiTrxRoute> Routes { get; set; } = [];
}
