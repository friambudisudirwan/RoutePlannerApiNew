using System;

namespace RoutePlanner_Api.Dtos;

public class VtsApiResponseBase<T>
{
    public int ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public T? Data { get; set; }
}
