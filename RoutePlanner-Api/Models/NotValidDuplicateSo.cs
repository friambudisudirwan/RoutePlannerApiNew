using System;

namespace RoutePlanner_Api.Models;

public class NotValidDuplicateSo
{
    public string? so_no { get; set; }
    public string? pl { get; set; }
    public string? ps { get; set; }
    public int duplicate_count { get; set; }
}
