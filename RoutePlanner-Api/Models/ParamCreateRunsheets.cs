using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

public class ParamCreateRunsheets
{
    [JsonPropertyName("user")]
    public required ConfMstUser User { get; set; }

    [JsonPropertyName("data")]
    public required List<ApiMstPool> Data { get; set; }
}
