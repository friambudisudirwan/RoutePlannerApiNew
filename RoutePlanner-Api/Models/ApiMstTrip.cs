using System;

namespace RoutePlanner_Api.Models;

public class ApiMstTrip
{
    public string RunID { get; set; } = string.Empty;
    public int SeqNo { get; set; }
    public required string TripId { get; set; }
    public required string TripName { get; set; }
    public required string TripLong { get; set; }
    public required string TripLat { get; set; }
    public required DateTime TimeOpen { get; set; }
    public required DateTime TimeClose { get; set; }
    public required int TimeWait { get; set; }
    public required int TimeOperation { get; set; }
    public required double Capacity { get; set; }
    public double Balance { get; set; }
    public string? LayananID { get; set; }
    public string? TripType { get; set; }
    public string? MetodeHitung { get; set; }
    public string? Siklus { get; set; }
    public string? TrxID { get; set; }
    public string? ZoneCode { get; set; }
    public string? RegionCode { get; set; }
    public string? Warehouse { get; set; }
    public string? NoSo { get; set; }
    public string? TypeArmada { get; set; }
    public string? CodeCustomer { get; set; }
    public string? BU { get; set; }
    public string? StorageType { get; set; }
    public string? Segment { get; set; }
    public double TotalQty { get; set; }
    public double TotalGrossVolume { get; set; }
}
