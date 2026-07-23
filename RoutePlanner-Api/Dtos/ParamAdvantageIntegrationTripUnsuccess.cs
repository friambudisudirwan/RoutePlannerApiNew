using System;

namespace RoutePlanner_Api.Dtos;

public class ParamAdvantageIntegrationTripUnsuccess
{
    public string? tripID { get; set; }
    public string? tripName { get; set; }
    public string? tripLong { get; set; }
    public string? tripLat { get; set; }
    public string? timeOpen { get; set; }
    public string? timeClose { get; set; }
    public int timeWait { get; set; }
    public int timeOperation { get; set; }
    public double capacity { get; set; }
    public double balance { get; set; }
    public string? layananID { get; set; }
    public string? parentID { get; set; }
    public int isDv { get; set; }
    public string? usrUpd { get; set; }
    public string? tripType { get; set; }
    public string? metodeHitung { get; set; }
    public string? siklus { get; set; }
    public string? trxID { get; set; }
    public string? zoneCode { get; set; }
    public string? regionCode { get; set; }
}

// "tripID":        "string",
// 					"tripName":      "string",
// 					"tripLong":      "string",
// 					"tripLat":       "string",
// 					"timeOpen":      "string",
// 					"timeClose":     "string",
// 					"timeWait":      0,
// 					"timeOperation": 0,
// 					"capacity":      0,
// 					"balance":       0,
// 					"layananID":     "string",
// 					"parentID":      "string",
// 					"isDv":          0,
// 					"usrUpd":        "string",
// 					"tripType":      "string",
// 					"metodeHitung":  "string",
// 					"siklus":        "string",
// 					"trxID":         "string",
// 					"zoneCode":      "string",
// 					"regionCode":    "string",