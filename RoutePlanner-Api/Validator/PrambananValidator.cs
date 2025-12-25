using System;
using System.Globalization;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Validator;

public class PrambananValidator
{
    public
    (
        bool result,
        string message,
        List<NotValidDuplicateSo> list_duplicate_so,
        List<NotValidLonLatSo> list_not_valid_lon_lat,
        List<ParamTripPrambanan> list_so
    ) ValidatePrambananSo(List<ParamTripPrambanan> list_so)
    {
        var find_duplicate_so = list_so.GroupBy(x => new { x.TrxID, x.PL, x.PS }).Where(g => g.Count() > 1).Select(x => new NotValidDuplicateSo
        {
            so_no = x.Key.TrxID,
            pl = x.Key.PL,
            ps = x.Key.PS,
            duplicate_count = x.Count()
        }).ToList();

        var find_not_valid_lon_lat = list_so.Where(x => !string.IsNullOrEmpty(x.TripLong) &&
                                                        !string.IsNullOrEmpty(x.TripLat) &&
                                                        !IsValidLonLatInIndonesia(x.TripLong, x.TripLat)
                                            ).Select(x => new NotValidLonLatSo
                                            {
                                                so_no = x.TrxID,
                                                address_id = x.TripId,
                                                address_name = x.TripName,
                                                warehouse_code = x.PoolID,
                                                lon = x.TripLong,
                                                lat = x.TripLat
                                            }).ToList();

        if (find_duplicate_so.Count > 0 || find_not_valid_lon_lat.Count > 0) return (false, "Bad Request", find_duplicate_so, find_not_valid_lon_lat, []);

        var list_valid_so = new List<ParamTripPrambanan>();

        list_valid_so.AddRange([.. list_so.Where(x => !string.IsNullOrEmpty(x.TripLong) && !string.IsNullOrEmpty(x.TripLat) && IsValidLonLatInIndonesia(x.TripLong, x.TripLat))]);
        list_valid_so.AddRange([.. list_so.Where(x => string.IsNullOrEmpty(x.TripLong) || string.IsNullOrEmpty(x.TripLat)).Select(x => x with
        {
            IsValidLonLat = 0,
            TripLong = string.Empty,
            TripLat = string.Empty
        })]);

        return (true, "Validation Success", [], [], list_valid_so);
    }

    private static bool IsValidLonLatInIndonesia(string lon, string lat)
    {
        // 1. null / empty check
        if (string.IsNullOrWhiteSpace(lon) || string.IsNullOrWhiteSpace(lat))
            return false;

        // 2. parse ke double (culture-invariant, penting!)
        if (!double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            return false;

        if (!double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
            return false;

        // 3. validasi range global
        if (longitude < -180 || longitude > 180)
            return false;

        if (latitude < -90 || latitude > 90)
            return false;

        // 4. validasi wilayah Indonesia
        if (longitude < 95.0 || longitude > 141.5)
            return false;

        if (latitude < -11.5 || latitude > 6.5)
            return false;

        return true;
    }
}
