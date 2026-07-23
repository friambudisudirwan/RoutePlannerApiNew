using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Dtos;

/// <summary>Request body for updating PL/PS on sales orders.</summary>
public class ParamUpdatePS
{
    /// <summary>Rows to update.</summary>
    [JsonPropertyName("data")]
    public required List<ParamUpdatePSItem> Data { get; set; }
}

/// <summary>Single SO PL/PS update row.</summary>
public class ParamUpdatePSItem
{
    /// <summary>Sales order number.</summary>
    [JsonPropertyName("so_no")]
    public required string SoNo { get; set; }

    /// <summary>PL value.</summary>
    [JsonPropertyName("pl")]
    public required string Pl { get; set; }

    /// <summary>PS value.</summary>
    [JsonPropertyName("ps")]
    public required string Ps { get; set; }
}
