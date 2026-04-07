using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Dtos;

public class ParamUpdatePS
{
    [JsonPropertyName("data")]
    public required List<ParamUpdatePSItem> Data { get; set; }
}

public class ParamUpdatePSItem
{
    [JsonPropertyName("so_no")]
    public required string SoNo { get; set; }

    [JsonPropertyName("pl")]
    public required string Pl { get; set; }

    [JsonPropertyName("ps")]
    public required string Ps { get; set; }
}
