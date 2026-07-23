using System;

namespace RoutePlanner_Api.Dtos;

public class ParamAdvantageIntegrationTripSuccess
{
    public string? runID { get; set; }
    public int routeNo { get; set; }
    public string? carID { get; set; }
    public string? policeNo { get; set; }
    public int capacityStart { get; set; }
    public int workingTimeStart { get; set; }
    public string? startTrxID { get; set; }
    public string? startID { get; set; }
    public string? startName { get; set; }
    public string? startLong { get; set; }
    public string? startLat { get; set; }
    public string? endTrxID { get; set; }
    public int endSeq { get; set; }
    public string? endID { get; set; }
    public string? endName { get; set; }
    public string? endLong { get; set; }
    public string? endLat { get; set; }
    public string? timeOpen { get; set; }
    public string? timeClose { get; set; }
    public int maxTimeIdle { get; set; }
    public string? startTime { get; set; }
    public int duration { get; set; }
    public string? arrivalTime { get; set; }
    public int idleTime { get; set; }
    public int timeWait { get; set; }
    public string? startOperationTime { get; set; }
    public int timeOperation { get; set; }
    public int timeRest { get; set; }
    public string? endTime { get; set; }
    public int workingTimeEnd{ get; set; }
    public double capacityUse{ get; set; }
    public double capacityEnd{ get; set; }
    public double distanace{ get; set; }
    public double balance{ get; set; }
    public string? layananID{ get; set; }
    public string? tripType{ get; set; }
    public string? metodeHitung{ get; set; }
    public string? siklus{ get; set; }
    public string? trxID{ get; set; }
    public string? zoneCode{ get; set; }
    public string? regionCode{ get; set; }
}