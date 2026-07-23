using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Dtos;

/// <summary>Runsheet + vehicle pair for TMS integration.</summary>
public record class ParamTrxRoute
{
    /// <summary>Runsheet ID.</summary>
    [JsonPropertyName("runid")]
    public required string RunId { get; set; }

    /// <summary>Vehicle / car ID.</summary>
    [JsonPropertyName("carid")]
    public required string CarId { get; set; }
}
