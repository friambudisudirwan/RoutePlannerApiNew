using System;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Exceptions;

public class PrambananSoValidationException(string message, List<NotValidDuplicateSo> list_duplicate_so, List<NotValidLonLatSo> list_not_valid_lon_lat) : Exception(message)
{
    public List<NotValidDuplicateSo> ListDuplicateSo { get; } = list_duplicate_so;
    public List<NotValidLonLatSo> ListNotValidLonLat { get; } = list_not_valid_lon_lat;
}
