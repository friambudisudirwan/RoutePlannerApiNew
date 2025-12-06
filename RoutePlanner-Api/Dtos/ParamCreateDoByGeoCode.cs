using System;

namespace RoutePlanner_Api.Dtos;

public class ParamCreateDoByGeoCode
{
    public long do_id { get; set; }
    public DateTime tgl_do { get; set; }
    public required string car_plate { get; set; }
    public required string no_do { get; set; }
    public required string note { get; set; }
    public int opsi_complete { get; set; }
    public int driver_id { get; set; }
    public required string driver_code { get; set; }
    public List<ParamCreateDoAsalTujuan> geo_asal { get; set; } = [];
    public List<ParamCreateDoAsalTujuan> geo_tujuan { get; set; } = [];
}
