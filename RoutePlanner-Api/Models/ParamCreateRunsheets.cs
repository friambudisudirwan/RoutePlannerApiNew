using System;

namespace RoutePlanner_Api.Models;

public class ParamCreateRunsheets
{
    public required ConfMstUser User { get; set; }
    public required List<ApiMstPool> Data { get; set; }
}
