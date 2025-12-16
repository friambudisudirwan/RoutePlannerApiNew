using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Dtos;

public record ParamTripPrambanan
{
    [JsonPropertyName("warehouse_code")]
    public required string PoolID { get; set; }
    public int? SeqNo { get; set; }

    [JsonPropertyName("so_no")]
    public required string TrxID { get; set; }

    [JsonPropertyName("address_id")]
    public required string TripId { get; set; }

    [JsonPropertyName("address_name")]
    public string? TripName { get; set; }

    [JsonPropertyName("city_name")]
    public string? CityName { get; set; }
    
    [JsonPropertyName("lon")]
    public required string TripLong { get; set; }

    [JsonPropertyName("lat")]
    public required string TripLat { get; set; }

    [JsonPropertyName("time_open")]
    public string? TimeOpen { get; set; }

    [JsonPropertyName("time_close")]
    public string? TimeClose { get; set; }

    [JsonPropertyName("time_wait")]
    public int? TimeWait { get; set; }

    [JsonPropertyName("time_operation")]
    public int? TimeOperation { get; set; }

    [JsonPropertyName("total_gross_weight")]
    public double Capacity { get; set; }

    [JsonPropertyName("balance")]
    public double Balance { get; set; }

    [JsonPropertyName("total_qty")]
    public double TotalQty { get; set; }

    [JsonPropertyName("total_gross_volume")]
    public double TotalGrossVolume { get; set; }

    [JsonPropertyName("no_so")]
    public string? NoSo { get; set; }

    [JsonPropertyName("customer_code")]
    public string? CodeCustomer { get; set; }

    [JsonPropertyName("bu")]
    public string? BU { get; set; }

    [JsonPropertyName("storage_type_code")]
    public string? StorageType { get; set; }

    [JsonPropertyName("segment")]
    public string? Segment { get; set; }

    [JsonPropertyName("pl")]
    public string? PL { get; set; }

    [JsonPropertyName("ps")]
    public string? PS { get; set; }

    public int IsValidLonLat { get; set; }
    public string? UsrUpd { get; set; }
    public DateTime? DtmUpd { get; set; }
}
