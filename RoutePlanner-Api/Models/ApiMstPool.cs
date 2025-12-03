using System;

namespace RoutePlanner_Api.Models;

public class ApiMstPool
{
    public string RunID { get; set; } = string.Empty;
    public required string PoolID { get; set; }
    public string PoolName { get; set; } = string.Empty;
    public required DateTime StartTime { get; set; }
    public required string StartLong { get; set; }
    public required string StartLat { get; set; }
    public int MaxTimeIdle { get; set; }
    public List<ApiMstCar> Cars { get; set; } = [];
    public List<ApiMstTrip> Trips { get; set; } = [];
}
