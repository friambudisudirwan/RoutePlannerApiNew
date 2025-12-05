namespace RoutePlanner_Api.Dtos;

public record class ParamIntegrateRunsheets
{
    public required List<ParamTrxRoute> data { get; set; }
}
