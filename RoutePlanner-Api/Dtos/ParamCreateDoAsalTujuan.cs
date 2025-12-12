using System;

namespace RoutePlanner_Api.Dtos;

public class ParamCreateDoAsalTujuan
{
    public required string code { get; set; }
    public string? no_sj { get; set; }
}
