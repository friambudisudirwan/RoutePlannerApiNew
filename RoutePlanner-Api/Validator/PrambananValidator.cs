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
        var list_not_valid_so = new List<NotValidLonLatSo>();
        var list_valid_so = new List<ParamTripPrambanan>();

        var find_duplicate_so = list_so.GroupBy(x => new { x.TrxID, x.PL, x.PS }).Where(g => g.Count() > 1).Select(x => new NotValidDuplicateSo
        {
            so_no = x.Key.TrxID,
            pl = x.Key.PL,
            ps = x.Key.PS,
            duplicate_count = x.Count()
        }).ToList();

        foreach (var so in list_so)
        {
            var lon = so.TripLong;
            var lat = so.TripLat;

            // cek apabila lon lat salahsatunya ada yang string empty, maka insert lon lat sebagai string empty (valid)
            if (string.IsNullOrWhiteSpace(lon) || string.IsNullOrWhiteSpace(lat))
            {
                list_valid_so.Add(so with
                {
                    TripLong = string.Empty,
                    TripLat = string.Empty,
                    IsValidLonLat = 0
                });
                continue;
            }

            // cari lon lat yang ngga valid (tidak di indonesia) (not valid), apabila diswap valid, makan hasil akan valid (valid)
            if (!TryNormalizeLonLatIndonesia(ref lon, ref lat))
            {
                // list_not_valid_so.Add(new NotValidLonLatSo
                // {
                //     so_no = so.TrxID,
                //     address_id = so.TripId,
                //     address_name = so.TripName,
                //     warehouse_code = so.PoolID,
                //     lon = so.TripLong,
                //     lat = so.TripLat
                // });

                list_valid_so.Add(so with
                {
                    TripLong = string.Empty,
                    TripLat = string.Empty,
                    IsValidLonLat = 0
                });
                continue;
            }

            // so yang sudah diswap by reference akan masuk ke list valid
            list_valid_so.Add(so with
            {
                TripLong = lon,
                TripLat = lat
            });
        }

        if (find_duplicate_so.Count > 0 || list_not_valid_so.Count > 0) return (false, "Bad Request", find_duplicate_so, list_not_valid_so, []);
        return (true, "Validation Success", [], [], list_valid_so);
    }

    private static bool TryNormalizeLonLatIndonesia
    (
        ref string lon,
        ref string lat
    )
    {
        // kosong / whitespace → sah, tidak diapa-apakan
        if (string.IsNullOrWhiteSpace(lon) || string.IsNullOrWhiteSpace(lat))
            return true;

        // as-is valid → aman
        if (IsValidLonLatInIndonesia(lon, lat))
            return true;

        // coba parse
        if (!double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lonVal))
            return false;

        if (!double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latVal))
            return false;

        // cek kalau ditukar jadi valid
        var swappedLon = latVal.ToString(CultureInfo.InvariantCulture);
        var swappedLat = lonVal.ToString(CultureInfo.InvariantCulture);

        if (IsValidLonLatInIndonesia(swappedLon, swappedLat))
        {
            lon = swappedLon;
            lat = swappedLat;
            return true;
        }

        return false;
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
