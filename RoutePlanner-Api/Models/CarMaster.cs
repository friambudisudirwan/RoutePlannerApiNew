using System;

namespace RoutePlanner_Api.Models;

public class CarMaster
{
    public string? car_plate { get; set; }
    public string? msisdn { get; set; }
    public string? vehicle_id { get; set; }
    public int driver_id { get; set; }
}
