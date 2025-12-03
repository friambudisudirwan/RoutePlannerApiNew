using System;

namespace RoutePlanner_Api.Models;

public class ApiMstCar
{
    public string RunID { get; set; } = string.Empty;
    public int SeqNo { get; set; }
    public required string CarID { get; set; }
    public string CarDesc { get; set; } = string.Empty;
    public required string PoliceNo { get; set; }
    public double Capacity { get; set; }
    public int WorkingTime { get; set; }
    public DateTime MinRestTime { get; set; }
    public int RestTime { get; set; }
}
