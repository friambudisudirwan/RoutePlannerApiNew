using RoutePlanner_Api.Dtos;

namespace RoutePlanner_Api.Exceptions;

public class UpdatePSNotFoundException(IReadOnlyList<ParamUpdatePSItem> listNotFoundSo)
    : Exception("Order tidak ditemukan di GPSB untuk satu atau lebih baris.")
{
    public IReadOnlyList<ParamUpdatePSItem> ListNotFoundSo { get; } = listNotFoundSo;
}
