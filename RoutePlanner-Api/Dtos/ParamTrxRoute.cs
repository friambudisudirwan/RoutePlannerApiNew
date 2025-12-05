using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Dtos;

public record class ParamTrxRoute
{
    [JsonPropertyName("runid")]
    public required string RunId { get; set; }
    [JsonPropertyName("carid")]
    public required string CarId { get; set; }
}
