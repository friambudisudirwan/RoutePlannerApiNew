using System;

namespace RoutePlanner_Api.Models;

public class GeofenceArea
{
    public int id { get; set; }
    public string? code { get; set; }
    public string? name { get; set; }
    public string? address { get; set; }
    public string? relative_name { get; set; }
    public string? color { get; set; }
    public int alert { get; set; }
    public int shapeid { get; set; }
    public string? points { get; set; }
    public double lon { get; set; }
    public double lat { get; set; }
    public double lon1 { get; set; }
    public DateTime? stamp { get; set; }
    public int companyid { get; set; }
    public int categoryid { get; set; }
    public int geo_type { get; set; }
    public int enabled_status { get; set; }
    public string? alert_telegram { get; set; }
    public string? alert_email { get; set; }
    public string? alert_over_time { get; set; }
    public int is_deleted { get; set; }
    public int usrupd { get; set; }
    public DateTime? dtmupd { get; set; }
}
