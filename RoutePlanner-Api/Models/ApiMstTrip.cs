using System;
using System.Text.Json.Serialization;

namespace RoutePlanner_Api.Models;

public record ApiMstTrip
{
    [JsonPropertyName("run_id")]
    public string? RunID { get; set; } = string.Empty;
    
    [JsonPropertyName("seq_no")]
    public int? SeqNo { get; set; }

    [JsonPropertyName("trip_id")]
    public required string TripId { get; set; }

    [JsonPropertyName("trip_name")]
    public string? TripName { get; set; }

    [JsonPropertyName("trip_address")]
    public string? TripAddress { get; set; }

    [JsonPropertyName("lon")]
    public string? TripLong { get; set; }

    [JsonPropertyName("lat")]
    public string? TripLat { get; set; }

    [JsonPropertyName("time_open")]
    public string? TimeOpen { get; set; }

    [JsonPropertyName("time_close")]
    public string? TimeClose { get; set; }

    [JsonPropertyName("time_wait")]
    public int TimeWait { get; set; }

    [JsonPropertyName("time_operation")]
    public int TimeOperation { get; set; }

    [JsonPropertyName("capacity")]
    public double? Capacity { get; set; }

    [JsonPropertyName("balance")]
    public double? Balance { get; set; }

    [JsonPropertyName("layanan_id")]
    public string? LayananID { get; set; }

    [JsonPropertyName("trip_type")]
    public string? TripType { get; set; }

    [JsonPropertyName("metode_hitung")]
    public string? MetodeHitung { get; set; }

    [JsonPropertyName("siklus")]
    public string? Siklus { get; set; }

    [JsonPropertyName("trx_id")]
    public string? TrxID { get; set; }

    [JsonPropertyName("zone_code")]
    public string? ZoneCode { get; set; }

    [JsonPropertyName("region_code")]
    public string? RegionCode { get; set; }

    [JsonPropertyName("code_customer")]
    public string? CodeCustomer { get; set; }
}
