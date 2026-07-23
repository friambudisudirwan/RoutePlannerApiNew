using System;

namespace RoutePlanner_Api.Dtos;

public class ResponseAdvantageIntegration
{
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public bool isSuccess { get; set; }
}
