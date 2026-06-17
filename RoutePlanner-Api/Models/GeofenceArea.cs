using System;

namespace RoutePlanner_Api.Models;

public class GeofenceArea
{
    public int id { get; set; }
    public string? code { get; set; }
    public string? name { get; set; }
    public string? points { get; set; }
    public double lon { get; set; }
    public double lat { get; set; }
}
