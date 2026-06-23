using System;

namespace RoutePlanner_Api.Models;

public class Client
{
    public int client_id { get; set; }
    public string? code { get; set; }
    public string? name { get; set; }
    public string? address { get; set; }
}
