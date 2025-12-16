using System;

namespace RoutePlanner_Api.Dtos;

public class ParamCreateDoAsalTujuan
{
    public required string code { get; set; }
    public string? no_sj { get; set; }
    public string? pl { get; set; }
    public string? ps { get; set; }
}
